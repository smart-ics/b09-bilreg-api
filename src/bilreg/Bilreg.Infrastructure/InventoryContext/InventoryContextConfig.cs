using Bilreg.Application.InventoryContext.LogistikMedisFeature;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;
using Bilreg.Infrastructure.InventoryContext.LogistikMedisFeature;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Repos;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Services;
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
        services.AddScoped<ITerimaMutasiDal, TerimaMutasiDal>();
        services.AddScoped<ITerimaMutasiItemDal, TerimaMutasiItemDal>();
        services.AddScoped<ITerimaMutasiRepo, TerimaMutasiRepo>();
        services.AddScoped<ITerimaMutasiMovementService, TerimaMutasiMovementService>();
        services.AddScoped<ISqlDatabase, SqlDatabase>();

        // LogistikMedisFeature (BFF)
        services.AddScoped<ILogistikMedisBffDal, LogistikMedisBffDal>();

        // OpnameFeature
        services.AddScoped<IStockOpnameSessionDal, StockOpnameSessionDal>();
        services.AddScoped<IStockOpnameScopeItemDal, StockOpnameScopeItemDal>();
        services.AddScoped<IStockOpnameCountedItemDal, StockOpnameCountedItemDal>();
        services.AddScoped<IStockOpnameVarianceItemDal, StockOpnameVarianceItemDal>();
        services.AddScoped<IStockOpnameRecountResultDal, StockOpnameRecountResultDal>();
        services.AddScoped<IStockOpnameAdjustmentDal, StockOpnameAdjustmentDal>();
        services.AddScoped<IStockOpnameSessionRepo, StockOpnameSessionRepo>();
        services.AddScoped<IStockOpnameAdjustmentRepo, StockOpnameAdjustmentRepo>();
        services.AddScoped<ISystemQuantityReconstructionService, SystemQuantityReconstructionService>();
        services.AddScoped<IOpnameItemInfoProvider, OpnameItemInfoProvider>();

        return services;
    }

    public static IServiceCollection AddMutasiBrgFeature(this IServiceCollection services)
    {
        return services.AddInventoryContext();
    }

    public static IServiceCollection AddOpnameFeature(this IServiceCollection services)
    {
        return services.AddInventoryContext();
    }
}
