using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public static class PakaiBedFeatureInfrastructureExtension
{
    public static IServiceCollection AddPakaiBedFeatureInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPakaiBedBillingLockCheckerDal, PakaiBedBillingLockCheckerDal>();
        services.AddScoped<IPakaiBedBillingLockChecker, PakaiBedBillingLockChecker>();
        services.AddScoped<IPakaiBedActiveOccupancyDal, PakaiBedActiveOccupancyDal>();
        return services;
    }

    public static IServiceCollection AddPakaiBedFeature(this IServiceCollection services)
    {
        return services.AddPakaiBedFeatureInfrastructure();
    }
}
