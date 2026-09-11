using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Wallet
{
    public class WalletService : IWalletService
    {
        private readonly AppDbContext _db;

        public WalletService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<WalletResponseDto> GetOrCreateWalletAsync(Guid userId)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new BookManagement.Repository.Entities.Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Balance = 0,
                    HeldBalance = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _db.Wallets.AddAsync(wallet);
                await _db.SaveChangesAsync();
            }

            return MapToDto(wallet);
        }

        public async Task<IEnumerable<EscrowResponseDto>> GetShopEscrowsAsync(Guid shopId)
        {
            var escrows = await _db.Escrows
                .AsNoTracking()
                .Where(e => e.ShopId == shopId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return escrows.Select(e => new EscrowResponseDto
            {
                Id = e.Id,
                OrderId = e.OrderId,
                ShopId = e.ShopId,
                Amount = e.Amount,
                PlatformFee = e.PlatformFee,
                NetShopEarnings = e.NetShopEarnings,
                ReleaseDate = e.ReleaseDate,
                Status = e.Status.ToString(),
                CreatedAt = e.CreatedAt
            });
        }

        public async Task CreateEscrowForOrderAsync(Guid orderId, Guid shopId, decimal amount, decimal platformFeeRate = 0.05m)
        {
            var existingEscrow = await _db.Escrows.FirstOrDefaultAsync(e => e.OrderId == orderId);
            if (existingEscrow != null) return;

            var platformFee = Math.Round(amount * platformFeeRate, 2);
            var netEarnings = amount - platformFee;

            var escrow = new Escrow
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ShopId = shopId,
                Amount = amount,
                PlatformFee = platformFee,
                NetShopEarnings = netEarnings,
                ReleaseDate = DateTimeOffset.UtcNow.AddDays(5),
                Status = EscrowStatus.HOLDING,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _db.Escrows.AddAsync(escrow);

            // Update shop held balance
            var shopWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == shopId);
            if (shopWallet == null)
            {
                shopWallet = new BookManagement.Repository.Entities.Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = shopId,
                    Balance = 0,
                    HeldBalance = netEarnings,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _db.Wallets.AddAsync(shopWallet);
            }
            else
            {
                shopWallet.HeldBalance += netEarnings;
                shopWallet.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        public async Task ReleaseEscrowAsync(Guid orderId)
        {
            var escrow = await _db.Escrows.FirstOrDefaultAsync(e => e.OrderId == orderId);
            if (escrow == null || escrow.Status != EscrowStatus.HOLDING) return;

            escrow.Status = EscrowStatus.RELEASED;
            escrow.UpdatedAt = DateTimeOffset.UtcNow;

            var shopWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == escrow.ShopId);
            if (shopWallet != null)
            {
                shopWallet.HeldBalance = Math.Max(0, shopWallet.HeldBalance - escrow.NetShopEarnings);
                shopWallet.Balance += escrow.NetShopEarnings;
                shopWallet.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // Create TransactionHistory record
            var tx = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                UserId = escrow.ShopId,
                Amount = escrow.NetShopEarnings,
                TransactionType = TransactionType.ESCROW_RELEASE,
                ReferenceType = ReferenceType.SHOP_REVENUE,
                TransactionCode = $"ESCROW-{orderId.ToString().Substring(0, 8).ToUpper()}",
                Description = $"Giải ngân tiền bán hàng từ đơn #{orderId.ToString().Substring(0, 8)}",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _db.TransactionHistories.AddAsync(tx);

            await _db.SaveChangesAsync();
        }

        public async Task RefundEscrowAsync(Guid orderId)
        {
            var escrow = await _db.Escrows.FirstOrDefaultAsync(e => e.OrderId == orderId);
            if (escrow == null || escrow.Status != EscrowStatus.HOLDING) return;

            escrow.Status = EscrowStatus.REFUNDED;
            escrow.UpdatedAt = DateTimeOffset.UtcNow;

            var shopWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == escrow.ShopId);
            if (shopWallet != null)
            {
                shopWallet.HeldBalance = Math.Max(0, shopWallet.HeldBalance - escrow.NetShopEarnings);
                shopWallet.UpdatedAt = DateTimeOffset.UtcNow;
            }

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order != null)
            {
                var customerWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == order.UserId);
                if (customerWallet == null)
                {
                    customerWallet = new BookManagement.Repository.Entities.Wallet
                    {
                        Id = Guid.NewGuid(),
                        UserId = order.UserId,
                        Balance = escrow.Amount,
                        HeldBalance = 0,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    await _db.Wallets.AddAsync(customerWallet);
                }
                else
                {
                    customerWallet.Balance += escrow.Amount;
                    customerWallet.UpdatedAt = DateTimeOffset.UtcNow;
                }

                // Create TransactionHistory record for customer refund
                var tx = new TransactionHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = order.UserId,
                    Amount = escrow.Amount,
                    TransactionType = TransactionType.REFUND,
                    ReferenceType = ReferenceType.REFUND,
                    TransactionCode = $"REFUND-{orderId.ToString().Substring(0, 8).ToUpper()}",
                    Description = $"Hoàn tiền vào ví điện tử từ đơn trả hàng #{orderId.ToString().Substring(0, 8)}",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _db.TransactionHistories.AddAsync(tx);
            }

            await _db.SaveChangesAsync();
        }

        public async Task WithdrawAsync(Guid userId, WithdrawRequest request)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Số tiền rút phải lớn hơn 0.");

            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null || wallet.Balance < request.Amount)
                throw new InvalidOperationException("Số dư khả dụng không đủ để thực hiện giao dịch rút tiền.");

            wallet.Balance -= request.Amount;
            wallet.UpdatedAt = DateTimeOffset.UtcNow;

            var tx = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = request.Amount,
                TransactionType = TransactionType.WITHDRAWAL,
                ReferenceType = ReferenceType.WITHDRAWAL,
                TransactionCode = $"WD-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                Description = $"Rút tiền về ngân hàng {request.BankName} - STK: {request.AccountNumber}",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _db.TransactionHistories.AddAsync(tx);

            await _db.SaveChangesAsync();
        }

        private static WalletResponseDto MapToDto(BookManagement.Repository.Entities.Wallet wallet) => new WalletResponseDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance,
            HeldBalance = wallet.HeldBalance,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.UpdatedAt
        };
    }
}
