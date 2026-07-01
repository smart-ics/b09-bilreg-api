using Farinv.Domain.Shared.Helpers;
using Farinv.Infrastructure.ParamContext;

namespace Farinv.Api.Configurations;

public static class DomainService
{
    public static IServiceCollection AddDomain(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services
            .AddScoped<ISequencerManual, SequencerManual>();
            ;
        
        return services;
    }    
}