using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.InventoryContext;

public static class InventoryContextConfig
{
    public static IServiceCollection AddInventoryContext(this IServiceCollection services)
    {
        services.AddScoped<IMutasiBrgDal, MutasiBrgDal>();
        services.AddScoped<IMutasiBrgItemDal, MutasiBrgItemDal>();
        services.AddScoped<IMutasiBrgRepo, MutasiBrgRepo>();
        services.AddScoped<IMutasiBrgMovementService, MutasiBrgMovementService>();
        services.AddScoped<ISqlDatabase, SqlDatabase>();

        return services;
    }

    public static IServiceCollection AddMutasiBrgFeature(this IServiceCollection services)
    {
        return services.AddInventoryContext();
    }
}
