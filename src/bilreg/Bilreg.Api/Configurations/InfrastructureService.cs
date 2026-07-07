using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.RolloutFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Infrastructure.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Infrastructure;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.Integration;
using Bilreg.Infrastructure.AdmisiRanapContext.RolloutFeature;
using Bilreg.Infrastructure.LabContext.Integration;
using Bilreg.Infrastructure.LabContext.LabOwareFeature;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using Bilreg.Application.PaymentContext.PasienBalanceFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.Shared;
using Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Infrastructure.Shared.Param;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.CleanArchHelper;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using Scrutor;

namespace Bilreg.Api.Configurations;

public static class InfrastructureService
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services
            .AddScoped<INunaCounterDal, ParamNoDal>()
            .AddScoped<INunaCounterDecDal, ParamNoDal>()
            .AddScoped<ITglJamProvider, TglJamProvider>()
            .AddScoped<ISequencer, Sequencer>()
            .AddScoped<IRestClientFactory, RestClientFactory>()
            .AddScoped<ILabOrderWorklistDal, LabOrderWorklistDal>()
            .AddScoped<ILabOrderReleaseWorklistDal, LabOrderReleaseWorklistDal>()
            .AddScoped<ILabResultVerificationWorklistDal, LabResultVerificationWorklistDal>()
            .AddScoped<ILabCollectionPreparationDal, LabCollectionPreparationDal>()
            .AddScoped<ILabRegIntegration, LabRegIntegration>()
            .AddScoped<ILabBillingIntegration, LabBillingIntegration>()
            .AddScoped<ILabTestResolutionService, LabTestResolutionService>()
            .AddScoped<ILabOwareIntegration, LabOwareIntegration>()
            .AddScoped<ILabOwareQueueWorklistDal, LabOwareQueueWorklistDal>()
            .AddScoped<IWaitingListWorklistDal, WaitingListWorklistDal>()
            .AddScoped<IDoctorServiceGateway, DoctorServiceGateway>()
            .AddScoped<IPatientAdministrationGateway, PatientAdministrationGateway>()
            .AddScoped<IWardAccommodationGateway, WardAccommodationGateway>()
            .AddScoped<IAdmisiRanapRolloutDal, AdmisiRanapRolloutDal>()
            .AddScoped<LabOwareQueueProcessor>()
            .AddScoped<ILabResultPdfRenderer, LabResultPdfRenderer>()
            .AddScoped<ILabResultScaffoldService, LabResultScaffoldService>()
            .AddScoped<ITarifPublishLogRepo, TarifPublishLogRepo>()
            .AddScoped<INilaiTarifProjectionWriter, NilaiTarifProjectionWriter>()
            .AddScoped<ITarifOperationalStateRepo, TarifOperationalStateRepo>()
            .AddScoped<ITarifOperationalStateDal, TarifOperationalStateDal>()
            .AddScoped<ITarifProjectionReadRepo, TarifProjectionReadRepo>()
            .AddScoped<ITarifMigrationModeResolver, TarifMigrationModeResolver>()
            .AddScoped<ITarifMigrationGuard, TarifMigrationGuard>()
            .AddScoped<IJadwalPraktekHarianRepo, JadwalPraktekHarianRepo>()
            .AddScoped<IPasienBalanceLegacyReader, LegacyOutstandingReceivableReader>()
            .AddScoped<IUnitOfWork, TransHelperUnitOfWork>()
            .AddScoped<ITransferReceivableService, TransferReceivableService>()
            .AddSingleton<TarifOperationalGate>()
            .AddMemoryCache();

        services
            .Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SECTION_NAME))
            .Configure<TarifMigrationOptions>(configuration.GetSection(TarifMigrationOptions.SECTION_NAME))
            .Configure<LabResultPdfOptions>(configuration.GetSection(LabResultPdfOptions.SECTION_NAME))
            .Configure<PasienContextOptions>(configuration.GetSection(PasienContextOptions.SECTION_NAME))
            .Configure<RemoteCetakOptions>(configuration.GetSection(RemoteCetakOptions.SECTION_NAME))
            .Configure<EmrOptions>(configuration.GetSection(EmrOptions.SECTION_NAME))
            .Configure<HiDokOptions>(configuration.GetSection(HiDokOptions.SECTION_NAME))
            .Configure<JetliOptions>(configuration.GetSection(JetliOptions.SECTION_NAME))
            .Configure<JknOptions>(configuration.GetSection(JknOptions.SECTION_NAME))
            .Configure<JadwalPraktekOptions>(configuration.GetSection(JadwalPraktekOptions.SECTION_NAME))
            .Configure<AdmisiRanapOptions>(configuration.GetSection(AdmisiRanapOptions.SECTION_NAME));

        services
            .Scan(selector => selector
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IInsert<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IUpdate<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IDelete<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IGetData<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListData<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListData<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListData<,,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListDataMayBe<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListDataMayBe<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(INunaService<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(INunaService<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IRequestResponseService<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(ISaveChange<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(ISaveChange<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IDeleteEntity<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(ILoadEntity<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
            );
        return services;
    }

}