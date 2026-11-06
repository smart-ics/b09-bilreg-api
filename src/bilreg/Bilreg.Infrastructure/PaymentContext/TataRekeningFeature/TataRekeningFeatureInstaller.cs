using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public static class TataRekeningFeatureInstaller
{
    public static IServiceCollection AddTataRekeningFeature(this IServiceCollection services)
    {
        services.AddScoped<IRegRelationPolicy, RegRelationPolicyAdapter>();
        services.AddScoped<IRegJaminanPolicy, RegJaminanPolicyAdapter>();
        return services;
    }
}
