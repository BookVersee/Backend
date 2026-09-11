using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Wallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookManagement.Service.BackgroundServices
{
    public class ReturnAndEscrowBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReturnAndEscrowBackgroundService> _logger;

        public ReturnAndEscrowBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ReturnAndEscrowBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReturnAndEscrowBackgroundService starting execution.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var walletService = scope.ServiceProvider.GetRequiredService<IWalletService>();

                    var now = DateTimeOffset.UtcNow;

                    // 1. Process Escrows ready to be released (5 days passed, no active return request)
                    var escrowsToRelease = await db.Escrows
                        .Where(e => e.Status == EscrowStatus.HOLDING && e.ReleaseDate <= now)
                        .ToListAsync(stoppingToken);

                    foreach (var escrow in escrowsToRelease)
                    {
                        var hasPendingReturn = await db.ReturnRequests.AnyAsync(rr =>
                            rr.OrderDetail.OrderId == escrow.OrderId &&
                            (rr.Status == ReturnRequestStatus.PENDING ||
                             rr.Status == ReturnRequestStatus.SHOP_REJECTED ||
                             rr.Status == ReturnRequestStatus.ESCALATED), stoppingToken);

                        if (!hasPendingReturn)
                        {
                            await walletService.ReleaseEscrowAsync(escrow.OrderId);
                            _logger.LogInformation("Escrow released for order {OrderId}", escrow.OrderId);
                        }
                    }

                    // 2. Process expired shop rejections (> 3 days with no customer escalation)
                    var threeDaysAgo = now.AddDays(-3);
                    var expiredRejections = await db.ReturnRequests
                        .Include(rr => rr.OrderDetail)
                        .Where(rr => rr.Status == ReturnRequestStatus.SHOP_REJECTED &&
                                    rr.ShopRespondedAt.HasValue &&
                                    rr.ShopRespondedAt.Value <= threeDaysAgo)
                        .ToListAsync(stoppingToken);

                    foreach (var req in expiredRejections)
                    {
                        req.Status = ReturnRequestStatus.EXPIRED;
                        req.UpdatedAt = now;
                        await walletService.ReleaseEscrowAsync(req.OrderDetail.OrderId);
                        _logger.LogInformation("Return request {Id} expired after 3 days without escalation.", req.Id);
                    }

                    if (expiredRejections.Any())
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }

                    // 3. Auto unlock shops whose 1-month lock period has expired
                    var expiredShopLocks = await db.Shops
                        .Where(s => s.Condition == ShopCondition.LOCKED &&
                                    s.LockedUntil.HasValue &&
                                    s.LockedUntil.Value <= now)
                        .ToListAsync(stoppingToken);

                    foreach (var shop in expiredShopLocks)
                    {
                        shop.Condition = ShopCondition.OPEN;
                        shop.LockedUntil = null;
                        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == shop.Id, stoppingToken);
                        if (user != null)
                        {
                            user.Role = UserRole.SHOP;
                        }
                        _logger.LogInformation("Shop {ShopId} automatically unlocked after lock period.", shop.Id);
                    }

                    if (expiredShopLocks.Any())
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in ReturnAndEscrowBackgroundService.");
                }

                // Check every 10 minutes
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
