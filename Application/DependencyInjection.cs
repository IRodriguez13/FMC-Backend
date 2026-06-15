using Fmc.Application.Configuration;
using Fmc.Application.Interfaces;
using Fmc.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fmc.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IConsumerAuthService, ConsumerAuthService>();
        services.AddScoped<IEnterpriseAuthService, EnterpriseAuthService>();
        services.AddScoped<IConsumerProfileService, ConsumerProfileService>();
        services.AddScoped<IEnterpriseCafeteriaService, EnterpriseCafeteriaService>();
        services.AddScoped<ICafeteriaDiscoveryService, CafeteriaDiscoveryService>();
        services.AddScoped<ICafeteriaPhotoService, CafeteriaPhotoService>();
        services.AddScoped<ICafeteriaReviewService, CafeteriaReviewService>();
        services.AddScoped<IConsumerFavoriteService, ConsumerFavoriteService>();
        services.AddScoped<IEnterpriseCouponService, EnterpriseCouponService>();
        services.AddScoped<ICafeteriaCouponService, CafeteriaCouponService>();
        services.AddScoped<IEnterpriseStatsService, EnterpriseStatsService>();
        return services;
    }
}
