using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

/// <summary>
/// Pendaftaran Dependency Injection seluruh komponen fitur
/// <c>ChargeContext.PricingBarangFeature</c> (DAL Dapper, repository, dan domain service kalkulasi
/// harga) pada container API. Mengikuti pola installer per-feature rumah
/// (<see cref="Bilreg.Infrastructure.BrgContext.BrgFeature.BrgFeatureInstaller"/>).
/// </summary>
public static class PricingBarangServiceRegistration
{
    public static IServiceCollection AddPricingBarangFeature(this IServiceCollection services)
    {
        // DAL Dapper (tabel legacy, Zero New Tables).
        services.AddScoped<IPricingBarangDal, PricingBarangDal>();
        services.AddScoped<IPricingBarangItemDal, PricingBarangItemDal>();
        services.AddScoped<ITipeHargaDal, TipeHargaDal>();
        services.AddScoped<IHnaBatchDal, HnaBatchDal>();
        services.AddScoped<IHnaBatchItemDal, HnaBatchItemDal>();
        services.AddScoped<IRoundingPolicyDal, RoundingPolicyDal>();
        services.AddScoped<IPurchaseReceiptHnaDal, PurchaseReceiptHnaDal>();
        services.AddScoped<IPurchaseReceiptHnaQuery>(
            sp => sp.GetRequiredService<IPurchaseReceiptHnaDal>());

        // Repository / port persistensi Application.
        services.AddScoped<IPricingBarangRepo, PricingBarangRepo>();
        services.AddScoped<ITipeHargaRepo, TipeHargaRepo>();
        services.AddScoped<IHnaBatchRepo, HnaBatchRepo>();
        services.AddScoped<IRoundingPolicyRepo, RoundingPolicyRepo>();

        // Domain service kalkulasi harga bersama (ARCHITECTURE TD-06).
        services.AddScoped<IItemPricingCalculator, ItemPricingCalculator>();

        return services;
    }
}
