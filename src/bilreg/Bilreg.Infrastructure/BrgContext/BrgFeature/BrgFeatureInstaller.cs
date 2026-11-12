using Bilreg.Application.BrgContext.BrgFeature;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.BrgContext.BrgFeature;

public static class BrgFeatureInstaller
{
    public static IServiceCollection AddBrgFeature(this IServiceCollection services)
    {
        services.AddScoped<IBrgStockMovementCheck, BrgStockMovementCheckDal>();
        services.AddScoped<IBrgRepo, BrgRepo>();
        services.AddScoped<IBrgDal, BrgDal>();
        services.AddScoped<IBrgSatuanDal, BrgSatuanDal>();
        return services;
    }
}
