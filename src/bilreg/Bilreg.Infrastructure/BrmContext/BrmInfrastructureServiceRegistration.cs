using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.BrmContext;

public static class BrmInfrastructureServiceRegistration
{
    public static IServiceCollection AddBrmInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IMutasiBerkasDal, MutasiBerkasDal>();
        services.AddScoped<IVolumeBerkasDal, VolumeBerkasDal>();
        services.AddScoped<ILokasiBerkasDal, LokasiBerkasDal>();
        services.AddScoped<IMutasiBerkasWorklistDal, MutasiBerkasWorklistDal>();
        services.AddScoped<IMutasiBerkasRepo, MutasiBerkasRepo>();
        services.AddScoped<IMutasiBerkasIdGenerator, MutasiBerkasIdGenerator>();
        return services;
    }
}
