using System;
using System.Threading;
using System.Threading.Tasks;
using BookManagement.Service.Payment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookManagement.Service.BackgroundServices
{
    /// Vị trí: Background Service Worker - Chạy độc lập dưới nền Server, định kỳ quét hủy đơn quá hạn và hoàn lại kho.
    public class OrderExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderExpirationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public OrderExpirationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<OrderExpirationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// Chức năng: Tiến trình tự động quét và hủy đơn hàng quá hạn 15 phút
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderExpirationBackgroundService đã khởi động.");

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

                    int expiredOrdersCount = await paymentService.ExpirePendingOrdersAsync(expiryMinutes: 15);
                    if (expiredOrdersCount > 0)
                    {
                        _logger.LogInformation("OrderExpirationBackgroundService: Đã tự động hủy {Count} đơn hàng quá hạn và hoàn trả tồn kho.", expiredOrdersCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét hủy đơn hàng quá hạn.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
