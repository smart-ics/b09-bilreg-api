using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.RolloutFeature;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.Integration;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.PaymentContext.PasienBalanceFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.Shared;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Infrastructure;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Api.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.Integration;
using Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.OperationalWorklistFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.RolloutFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.LabContext.Integration;
using Bilreg.Infrastructure.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.LabContext.LabOwareFeature;
using Bilreg.Infrastructure.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Infrastructure.Shared.Param;
using Bilreg.Infrastructure.Shared.User;
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
            .AddScoped<ISqlServerClock, SqlServerClock>()
            .AddScoped<TglJamProvider>()
            .AddScoped<ITglJamProvider>(sp => sp.GetRequiredService<TglJamProvider>())
            .AddScoped<IBusinessDateStatus>(sp => sp.GetRequiredService<TglJamProvider>())
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
            .AddScoped<IEmrAntrianOutboundIntegration, EmrAntrianOutboundIntegration>()
            .AddScoped<IEmrAntrianOutboundWorklistDal, EmrAntrianOutboundWorklistDal>()
            .AddScoped<EmrAntrianOutboundProcessor>()
            .AddScoped<EmrAntrianOutboundEnqueueService>()
            .AddScoped<IWaitingListWorklistDal, WaitingListWorklistDal>()
            .AddScoped<IRegistrationCancellationEligibilityDal, RegistrationCancellationEligibilityDal>()
            .AddScoped<IRegistrationCancellationEligibilityRepo, RegistrationCancellationEligibilityRepo>()
            .AddScoped<IRegistrationHistoryReader, RegistrationHistoryReader>()
            .AddScoped<ICoordinatedCancellationRepo, CoordinatedCancellationRepo>()
            .AddScoped<IOperationalWorklistDal, OperationalWorklistDal>()
            .AddScoped<IJourneyDal, JourneyDal>()
            .AddScoped<IDoctorServiceGateway, DoctorServiceGateway>()
            .AddScoped<IPatientAdministrationGateway, PatientAdministrationGateway>()
            .AddScoped<IWardAccommodationGateway, WardAccommodationGateway>()
            .AddScoped<IBangsalByKelasDkDal, BangsalByKelasDkDal>()
            .AddScoped<IAdmisiRanapRolloutDal, AdmisiRanapRolloutDal>()
            .AddScoped<IAdmissionQueueRolloutDal, AdmissionQueueRolloutDal>()
            .AddScoped<IAdmissionQueueRolloutRepo, AdmissionQueueRolloutRepo>()
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
            .AddScoped<IUsmanGetTokenService,  UsmanGetTokenService>()
            .AddSingleton<TarifOperationalGate>()
            .AddMemoryCache();

        services
            .Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SECTION_NAME))
            .AddSingleton<Microsoft.Extensions.Options.IValidateOptions<BusinessDateOptions>>(
                new BusinessDateOptionsValidator())
            .AddOptions<BusinessDateOptions>()
                .Bind(configuration.GetSection(BusinessDateOptions.SECTION_NAME))
                .ValidateOnStart();

        services
            .Configure<TarifMigrationOptions>(configuration.GetSection(TarifMigrationOptions.SECTION_NAME))
            .Configure<LabResultPdfOptions>(configuration.GetSection(LabResultPdfOptions.SECTION_NAME))
            .Configure<PasienContextOptions>(configuration.GetSection(PasienContextOptions.SECTION_NAME))
            .Configure<RemoteCetakOptions>(configuration.GetSection(RemoteCetakOptions.SECTION_NAME))
            .Configure<EmrOptions>(configuration.GetSection(EmrOptions.SECTION_NAME))
            .Configure<HiDokOptions>(configuration.GetSection(HiDokOptions.SECTION_NAME))
            .Configure<JetliOptions>(configuration.GetSection(JetliOptions.SECTION_NAME))
            .Configure<JknOptions>(configuration.GetSection(JknOptions.SECTION_NAME))
            .Configure<JadwalPraktekOptions>(configuration.GetSection(JadwalPraktekOptions.SECTION_NAME))
            .Configure<AdmisiRanapOptions>(configuration.GetSection(AdmisiRanapOptions.SECTION_NAME))
            .Configure<UsmanOptions>(configuration.GetSection(UsmanOptions.SECTION_NAME));

        services.AddScoped<
            IAdmissionQueueOperationalProjection,
            AdmissionQueueOperationalProjection>();
        services.AddScoped<
            IAdmissionQueueOperationRepo,
            AdmissionQueueOperationRepo>();
        services.AddScoped<IAdmissionQueueClosingRepo, AdmissionQueueClosingRepo>();
        services.AddScoped<IAdmissionServicePointDal, AdmissionServicePointDal>();
        services.AddScoped<IAdmissionServicePointRepo, AdmissionServicePointRepo>();
        services.AddScoped<IAdmissionWorkstationDal, AdmissionWorkstationDal>();
        services.AddScoped<IAdmissionWorkstationRepo, AdmissionWorkstationRepo>();
        services.AddScoped<IAdmissionQueueDisplayDal, AdmissionQueueDisplayDal>();
        services.AddScoped<IAdmissionQueueDisplayRepo, AdmissionQueueDisplayRepo>();
        services.AddScoped<IAdmissionQueueKioskDal, AdmissionQueueKioskDal>();
        services.AddScoped<IAdmissionQueueKioskRepo, AdmissionQueueKioskRepo>();
        services.AddScoped<IAdmissionConfigurationAuditReader, AdmissionConfigurationAuditReader>();
        services.AddScoped<IAdmissionQueueWorkstationResolver, AdmissionQueueWorkstationResolver>();
        services.AddScoped<
            IBookingAssistanceRepo,
            BookingAssistanceRepo>();
        services.AddScoped<
            IRegistrationOutcomeOperationRepo,
            RegistrationOutcomeOperationRepo>();
        services.AddScoped<
            IAdmisiRajalOfficerWorklistReferenceReader,
            AdmisiRajalOfficerWorklistReferenceReader>();

        // Stock Ledger v2 — ports/repos not covered by Scrutor Nuna markers (S1-C1)
        services.AddScoped<ILegacyStockReadPort, LegacyStockReadPort>();
        services.AddScoped<IStockMutasiRepo, StockMutasiRepo>();
        services.AddScoped<IStockLegacyBindingRepo, StockLegacyBindingRepo>();

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
