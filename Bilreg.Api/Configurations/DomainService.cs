using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Helpers;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Helpers;
using Bilreg.Infrastructure.ParamContext;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using FluentValidation;
using System.Reflection;

namespace Bilreg.Api.Configurations;

public static class DomainService
{
    private const string DOMAIN_ASSEMBLY = "Bilreg.Domain";
    public static IServiceCollection AddDomain(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services
            .AddValidatorsFromAssembly(Assembly.Load(DOMAIN_ASSEMBLY))
            .AddScoped<IAntrianFactory, AntrianFactory>()
            .AddScoped<IJadwalPraktekFactory, JadwalPraktekFactory>()
            .AddScoped<IPasienFactory, PasienFactory>()
            .AddScoped<ISequencerManual, SequencerManual>()
            .AddScoped<IGetKodeRsService, GetKodeRsService>()
            .AddScoped<IRegFactory, RegFactory>()
            .AddScoped<IGetKelasRajalService, GetKelasRajalService>()
            .AddScoped<IGetSatuanTugasMedisService, GetSatuanTugasMedisService>()
            .AddScoped<IPolisFactory, PolisFactory>()
            ;
        
        return services;
    }    
}