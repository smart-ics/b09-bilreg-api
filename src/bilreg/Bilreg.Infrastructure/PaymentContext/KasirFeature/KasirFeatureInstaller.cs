using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public static class KasirFeatureInstaller
{
    public static IServiceCollection AddKasirFeature(this IServiceCollection services)
    {
        services.AddScoped<ITrsKasirDal, TrsKasirDal>();
        services.AddScoped<IKasirShiftTotalsDal, TrsKasirDal>();
        services.AddScoped<IKasirShiftDal, KasirShiftDal>();
        services.AddScoped<ITrsKasirRepo, TrsKasirRepo>();
        services.AddScoped<IKasirShiftRepo, KasirShiftRepo>();
        services.AddScoped<IKasirOrderInboxDal, KasirOrderInboxDal>();
        services.AddScoped<IKasirStatusSyncDal, KasirStatusSyncDal>();
        services.AddScoped<ILoketKasirDal, LoketKasirDal>();
        services.AddScoped<ITrsJenisPaymentDal, TrsJenisPaymentDal>();
        services.AddScoped<ITrsKasirItemDal, TrsKasirItemDal>();
        services.AddScoped<ITrsKasirLegacyDal, TrsKasirLegacyDal>();
        services.AddScoped<IKasirSupervisorAuthDal, KasirSupervisorAuthDal>();
        return services;
    }
}
