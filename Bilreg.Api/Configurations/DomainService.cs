using System.Reflection;
using Bilreg.Infrastructure.Helpers;
using FluentValidation;

namespace Bilreg.Api.Configurations;

public static class DomainService
{
    private const string DOMAIN_ASSEMBLY = "Bilreg.Domain";
    public static IServiceCollection AddDomain(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services
            .AddValidatorsFromAssembly(Assembly.Load(DOMAIN_ASSEMBLY));
        
        return services;
    }    
}