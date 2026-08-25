using BookManagement.Service.Book;
using BookManagement.Service.Chat;
using BookManagement.Service.Delivery;
using BookManagement.Service.Feedback;
using BookManagement.Service.Order;
using BookManagement.Service.Payment;
using BookManagement.Service.Shipping;
using BookManagement.Service.Shop;
using Microsoft.Extensions.DependencyInjection;

namespace BookManagement.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Entity-based service registrations (new pattern — interface → implementation)
        services.AddScoped<IShopService, BookManagement.Service.Shop.ShopService>();
        services.AddScoped<IBookService, BookManagement.Service.Book.BookService>();
        services.AddScoped<IOrderService, BookManagement.Service.Order.OrderService>();
        services.AddScoped<IFeedbackService, BookManagement.Service.Feedback.FeedbackService>();
        services.AddScoped<IDeliveryService, BookManagement.Service.Delivery.DeliveryService>();
        services.AddScoped<IShippingService, BookManagement.Service.Shipping.ShippingService>();
        services.AddScoped<IPaymentService, BookManagement.Service.Payment.PaymentService>();
        services.AddScoped<IChatService, BookManagement.Service.Chat.ChatService>();

        // Infrastructure/helper services (no interface needed)
        services.AddScoped<Services.VnpayService>();
        services.AddHttpClient<Services.GhnService>();

        // Legacy monolith ShopService — controllers still inject until fully migrated
        services.AddScoped<Services.ShopService>();
        services.AddScoped<Services.DeliveryService>();
        services.AddScoped<Services.ShippingService>();
        services.AddScoped<Services.PaymentService>();
        services.AddScoped<Services.ChatService>();

        return services;
    }
}
