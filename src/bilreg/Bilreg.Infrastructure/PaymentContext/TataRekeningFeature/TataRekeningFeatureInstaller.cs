using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public static class TataRekeningFeatureInstaller
{
    public static IServiceCollection AddTataRekeningFeature(this IServiceCollection services)
    {
        services.AddScoped<IRegRelationPolicy, RegRelationPolicyAdapter>();
        services.AddScoped<IRegJaminanPolicy, RegJaminanPolicyAdapter>();
        services.TryAddScoped<IRegAktifRepo, RegAktifRepo>();
        return services;
    }
}
