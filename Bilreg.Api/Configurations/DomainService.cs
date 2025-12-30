using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Param;
using Bilreg.Infrastructure.Shared.Param;

namespace Bilreg.Api.Configurations;

public static class DomainService
{
    private const string DOMAIN_ASSEMBLY = "Bilreg.Domain";
    public static IServiceCollection AddDomain(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services
            .AddScoped<IAntrianFactory, AntrianFactory>()
            .AddScoped<IJadwalPraktekFactory, JadwalPraktekFactory>()
            .AddScoped<IPasienFactory, PasienFactory>()
            .AddScoped<ISequencerManual, SequencerManual>()
            .AddScoped<IGetKodeRsService, GetKodeRsService>()
            .AddScoped<IRegFactory, RegFactory>()
            .AddScoped<IGetKelasRajalService, GetKelasRajalService>()
            .AddScoped<IPolisFactory, PolisFactory>()
            .AddScoped<IDeleteBookingWorkflow, DeleteBookingWorkflow>()
            .AddScoped<ITindakanFactory, TindakanFactory>()
            ;
        
        return services;
    }    
}