using BookManagement.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookManagement.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ShopService>();
        services.AddScoped<DeliveryService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<VnpayService>();
        services.AddScoped<ShippingService>();
        services.AddHttpClient<GhnService>();
        services.AddScoped<ChatService>();

        return services;
    }
}
