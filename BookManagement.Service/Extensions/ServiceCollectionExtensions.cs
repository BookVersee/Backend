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
        services.AddHttpClient<MomoService>();
        services.AddHttpClient<GhnService>();

        return services;
    }
}
