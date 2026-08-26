using BookManagement.Service.Book;
using BookManagement.Service.Feedback;
using BookManagement.Service.Order;
using BookManagement.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookManagement.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Public & Customer facing Services
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IFeedbackService, FeedbackService>();

        // Seller & Infrastructure Services
        services.AddScoped<ShopService>();
        services.AddScoped<DeliveryService>();
        services.AddScoped<ShippingService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<ChatService>();
        services.AddScoped<VnpayService>();
        services.AddHttpClient<GhnService>();

        return services;
    }
}
