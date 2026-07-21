using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Param;
using Bilreg.Infrastructure.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
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
            .AddScoped<IJadwalPraktekResolver, JadwalPraktekResolver>()
            .AddScoped<IJadwalPraktekFeatureResolver, JadwalPraktekFeatureResolver>()
            .AddScoped<IJadwalPraktekHarianOverrideGuard, JadwalPraktekHarianOverrideGuard>()
            .AddScoped<IPasienFactory, PasienFactory>()
            .AddScoped<ISequencerManual, SequencerManual>()
            .AddScoped<IGetKodeRsService, GetKodeRsService>()
            .AddScoped<IGetProjectIdService, GetProjectIdService>()
            .AddScoped<IRegFactory, RegFactory>()
            .AddScoped<IGetKelasRajalService, GetKelasRajalService>()
            .AddScoped<IPolisFactory, PolisFactory>()
            .AddScoped<IDeleteBookingWorkflow, DeleteBookingWorkflow>()
            .AddScoped<IGetAppSettingService, GetAppSettingService>()
            .AddScoped<IAddAntrianEmrByBookingService, AddAntrianEmrByBookingService>()
            .AddScoped<IAddAntrianEmrByRegService, AddAntrianEmrByRegService>()
            .AddScoped<IDashboardEmrRemoveRegService,  DashboardEmrRemoveRegService>()
            .AddScoped<IBridgeOperasiSaveService, BridgeOperasiSaveService>()
            .AddScoped<IDashboardEmrRemoveBookingService, DashboardEmrRemoveBookingService>()
            .AddScoped<IGetKelasRadarService, GetKelasRadarService>()
            .AddScoped<IAntrianMapWithBookingResolver, AntrianMapWithBookingResolver>()
            .AddScoped<IAntrianMapWithRegResolver, AntrianMapWithRegResolver>()
            .Configure<QueueNumberCompatibilityOptions>(configuration.GetSection(QueueNumberCompatibilityOptions.SectionName))
            .AddScoped<IQueueNumberCompatibilityAdapter, QueueNumberCompatibilityAdapter>()
            .AddScoped<ICreateBillDomService, CreateBillDomService>()
            .AddScoped<IProjectionRegenerationDomainService, ProjectionRegenerationDomainService>()
            .AddScoped<IMergeBillingDomainService, MergeBillingDomainService>()
            .AddScoped<IFinancialVerificationDomainService, FinancialVerificationDomainService>()
            .AddScoped<IFinancialAdjustmentDomainService, FinancialAdjustmentDomainService>()
            ;

        return services;
    }    
}