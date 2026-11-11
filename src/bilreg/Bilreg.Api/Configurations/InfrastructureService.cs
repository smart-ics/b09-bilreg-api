using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.RolloutFeature;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.Integration;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature.Handlers;
using Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Application.ApotekContext.WorklistFeature;
using Bilreg.Api.Hubs;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.ApotekContext.WorklistFeature;
using Bilreg.Infrastructure.ApotekContext.PenjualanFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.PaymentContext.PasienBalanceFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.Shared;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Infrastructure;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Infrastructure.InventoryContext;
using Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Api.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.Integration;
using Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.OperationalWorklistFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.RolloutFeature;
using Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.IgdContext.Integration;
using Bilreg.Infrastructure.LabContext;
using Bilreg.Infrastructure.LabContext.Integration;
using Bilreg.Infrastructure.LabContext.LabExternalFeature;
using Bilreg.Infrastructure.LabContext.LabMasterFeature;
using Bilreg.Infrastructure.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.LabContext.LabOwareFeature;
using Bilreg.Infrastructure.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.AccountingContext.JurnalFeature;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using Bilreg.Application.BedUsageContext.Integration;
using Bilreg.Infrastructure.BedUsageContext.Integration;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using Bilreg.Infrastructure.BrgContext.PricingPolicyFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Application.SalesContext.ResepFeature.UseCases;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.SalesContext.ResepFeature;
using Bilreg.Infrastructure.SalesContext.PenjualanFeature;
using Bilreg.Infrastructure.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Infrastructure.Shared.Param;
using Bilreg.Infrastructure.Shared.User;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.CleanArchHelper;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using Scrutor;
using Bilreg.Api.HostedServices;

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
            .AddScoped<TglJamProvider>(sp => new TglJamProvider(
                sp.GetRequiredService<ISqlServerClock>(),
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsSnapshot<BusinessDateOptions>>()))
            .AddScoped<ITglJamProvider>(sp => sp.GetRequiredService<TglJamProvider>())
            .AddScoped<IBusinessDateStatus>(sp => sp.GetRequiredService<TglJamProvider>())
            .AddScoped<ISequencer, Sequencer>()
            .AddScoped<ISequencerManual, SequencerManual>()
            .AddScoped<ILabOrderRepo, LabOrderRepo>()
            .AddScoped<IRegAktifRepo, RegAktifRepo>()
            .AddScoped<Bilreg.Application.AdmisiContext.LayananFeature.ILayananRepo, Bilreg.Infrastructure.AdmisiContext.LayananFeature.LayananRepo>()
            .AddScoped<IRestClientFactory, RestClientFactory>()
            .AddScoped<ILabOrderDal, LabOrderDal>()
            .AddScoped<ILabOrderTableDal, LabOrderDal>()
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
            .AddScoped<AptIntegrationWorker>()
            .AddScoped<ICollectionWindowDaysProvider, CollectionWindowDaysProvider>()
            .AddScoped<IAvailableStockPort, FailClosedAvailableStockPort>()
            .AddScoped<IAptAuthorizationPolicy, AuthenticatedActorAuthorizationPolicy>()
            .AddScoped<IKemkesKajianResepDal, KemkesKajianResepDal>()
            .AddScoped<IKemkesKajianResepCatalogPort, KemkesKajianResepCatalogAdapter>()
            .AddScoped<IResepVoidDal, Bilreg.Infrastructure.SalesContext.ResepFeature.ResepDal>()
            .AddScoped<IResepCreateDal, Bilreg.Infrastructure.SalesContext.ResepFeature.ResepDal>()
            .AddScoped<Bilreg.Infrastructure.SalesContext.ResepFeature.IResepDal, Bilreg.Infrastructure.SalesContext.ResepFeature.ResepDal>()
            .AddScoped<Bilreg.Application.SalesContext.ResepFeature.IResepDal, Bilreg.Infrastructure.SalesContext.ResepFeature.ResepDal>()
            .AddScoped<Bilreg.Application.ApotekContext.InvoiceFeature.IPaymentClearancePort, Bilreg.Application.ApotekContext.Shared.FailClosedPaymentClearancePort>()
            .AddScoped<Bilreg.Application.ApotekContext.QueueFeature.IPaymentClearancePort, FailClosedPharmacyPaymentClearancePort>()
            .AddScoped<IPharmacyQueuePublisher, SignalRPharmacyQueuePublisher>()
            .AddScoped<ITataRekeningChargePort, FailClosedTataRekeningChargePort>()
            .AddScoped<ITataRekeningInvoicePermissionPort, DenyTataRekeningInvoicePermissionPort>()
            .AddScoped<ISepFornasPort, FailClosedSepFornasPort>()
            .AddScoped<IIterConsumePort, FailClosedIterConsumePort>()
            .AddScoped<IMedicationPricePort, FailClosedMedicationPricePort>()
            .AddScoped<ITrackerPharmacyPort, TrackerPharmacyAdapter>()
            .AddScoped<IStockPharmacyPort, StockPharmacyAdapter>()
            .AddScoped<DispenseAuthorizedPolicy>()
            .AddScoped<IAptWorklistDal, AptWorklistDal>()
            .AddScoped<IAptIntegrationOpsDal, AptIntegrationOpsDal>()
            .AddScoped<IAptIntegrationHandler, TrackerServedAtHandler>()
            .AddScoped<IAptIntegrationHandler, TrackerDoneAtPickupHandler>()
            .AddScoped<IAptIntegrationHandler, TrackerDoneAtNoShowHandler>()
            .AddScoped<IAptIntegrationHandler, TrackerWithdrawnHandler>()
            .AddScoped<IAptIntegrationHandler, StockReserveHandler>()
            .AddScoped<IAptIntegrationHandler, StockRemoveOnHandoverHandler>()
            .AddScoped<IAptIntegrationHandler, StockReturnNoShowHandler>()
            .AddScoped<IAptIntegrationHandler, BillingChargeHandler>()
            .AddScoped<IAptIntegrationHandler, IterConsumeHandler>()
            .AddScoped<Bilreg.Application.BedUsageContext.WaitingListFeature.IWaitingListWorklistDal, Bilreg.Infrastructure.BedUsageContext.WaitingListFeature.WaitingListWorklistDal>()
            .AddScoped<Bilreg.Application.BedUsageContext.WaitingListFeature.IWaitingListRepo, Bilreg.Infrastructure.BedUsageContext.WaitingListFeature.WaitingListRepo>()
            .AddScoped<Bilreg.Application.BedUsageContext.WardCockpitFeature.IWardCockpitDal, Bilreg.Infrastructure.BedUsageContext.WardCockpitFeature.WardCockpitDal>()
            .AddScoped<IBedOperationalDal, BedOperationalDal>()
            .AddScoped<IBedReadinessTransactionDal, BedReadinessTransactionDal>()
            .AddScoped<IBedReadinessCorrectionDal, BedReadinessCorrectionDal>()
            .AddScoped<IBedOperationalRepo, BedOperationalRepo>()
            .AddScoped<IRegistrationCancellationEligibilityDal, RegistrationCancellationEligibilityDal>()
            .AddScoped<IRegistrationCancellationEligibilityRepo, RegistrationCancellationEligibilityRepo>()
            .AddScoped<IRegistrationHistoryReader, RegistrationHistoryReader>()
            .AddScoped<ICoordinatedCancellationRepo, CoordinatedCancellationRepo>()
            .AddScoped<IInpatientOrderDal, InpatientOrderDal>()
            .AddScoped<IInpatientOrderRepo, InpatientOrderRepo>()
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
            .AddScoped<INilaiTarifRepo, NilaiTarifRepo>()
            .AddScoped<INilaiTarifDal, NilaiTarifDal>()
            .AddScoped<INilaiTarifKompDal, NilaiTarifKompDal>()
            .AddScoped<IRegDal, RegDal>()
            .AddScoped<IRegRepo, RegRepo>()
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
            .AddScoped<ISmassTokenService, SmassTokenService>()
            .AddScoped<ISmassAssessmentGateway, SmassAssessmentGateway>()
            .AddScoped<IEmrLabelGateway, EmrLabelGateway>()
            .AddScoped<IEmr20AntrianRanapGateway, Emr20AntrianRanapGateway>()
            .AddScoped<IAktivasiBerkasEmrGateway, AktivasiBerkasEmrGateway>()
            .AddScoped<IPasienSasetDal, PasienSasetDal>()
            .AddScoped<Bilreg.Application.ChargeContext.RoomChargeFeature.IRoomChargeRepo, Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.RoomChargeRepo>()
            .AddScoped<Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.IRoomChargeDal, Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.RoomChargeDal>()
            .AddScoped<Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.IRoomChargeKomponenDal, Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.RoomChargeKomponenDal>()
            .AddScoped<Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.IRoomChargeLegacyDal, Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.RoomChargeLegacyDal>()
            .AddScoped<IRegDal, RegDal>()
            .AddScoped<IRegRepo, RegRepo>()
            .AddScoped<Bilreg.Application.PaymentContext.TataRekeningFeature.IMergeBillingRepo, Bilreg.Infrastructure.PaymentContext.TataRekeningFeature.MergeBillingRepo>()
            .AddScoped<Bilreg.Infrastructure.PaymentContext.TataRekeningFeature.IBilrgTrsMergeDal, Bilreg.Infrastructure.PaymentContext.TataRekeningFeature.BilrgTrsMergeDal>()
            .AddScoped<Bilreg.Domain.PaymentContext.TataRekeningFeature.IDownstreamSettlementInspector, Bilreg.Infrastructure.PaymentContext.TataRekeningFeature.DownstreamSettlementInspector>()
            .AddScoped<Bilreg.Domain.PaymentContext.TataRekeningFeature.IRoomChargeCompletenessQuery, Bilreg.Infrastructure.PaymentContext.TataRekeningFeature.RoomChargeCompletenessQuery>()
            .AddScoped<Bilreg.Application.Shared.BillingLockFeature.IBillingLockValidationService, Bilreg.Infrastructure.Shared.BillingLockFeature.BillingLockValidationService>()
            .AddScoped<Bilreg.Application.ChargeContext.RoomChargeFeature.IBillingLockChecker, Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.BillingLockChecker>()
            .AddScoped<Bilreg.Application.BedUsageContext.PakaiBedFeature.IPakaiBedBillingLockChecker, Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature.PakaiBedBillingLockChecker>()
            .AddScoped<Bilreg.Application.ChargeContext.RoomChargeFeature.IBedOccupancyQuery, Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.BedOccupancyQuery>()
            .AddScoped<Bilreg.Application.ChargeContext.RoomChargeFeature.IRoomChargeTarifService, Bilreg.Infrastructure.ChargeContext.RoomChargeFeature.RoomChargeTarifService>()
            .AddSingleton<TarifOperationalGate>()
            .AddMemoryCache();

        services.AddHttpClient("OwareClient", (sp, client) =>
        {
            var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OwareOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opt.BaseUrl))
            {
                client.BaseAddress = new Uri(opt.BaseUrl);
            }
            var timeoutSec = opt.TimeoutSeconds > 0 ? opt.TimeoutSeconds : 10;
            client.Timeout = TimeSpan.FromSeconds(timeoutSec);
        });

        services.AddHostedService<LabOwareOutboundWorker>();

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
            .Configure<Emr20Options>(configuration.GetSection(Emr20Options.SECTION_NAME))
            .Configure<HiDokOptions>(configuration.GetSection(HiDokOptions.SECTION_NAME))
            .Configure<JetliOptions>(configuration.GetSection(JetliOptions.SECTION_NAME))
            .Configure<JknOptions>(configuration.GetSection(JknOptions.SECTION_NAME))
            .Configure<JadwalPraktekOptions>(configuration.GetSection(JadwalPraktekOptions.SECTION_NAME))
            .Configure<AdmisiRanapOptions>(configuration.GetSection(AdmisiRanapOptions.SECTION_NAME))
            .Configure<UsmanOptions>(configuration.GetSection(UsmanOptions.SECTION_NAME))
            .Configure<SmassOptions>(configuration.GetSection(SmassOptions.SECTION_NAME))
            .Configure<IgdVisitOptions>(configuration.GetSection(IgdVisitOptions.SECTION_NAME))
            .Configure<OwareOptions>(configuration.GetSection(OwareOptions.SECTION_NAME))
            .Configure<LabOptions>(configuration.GetSection(LabOptions.SECTION_NAME))
            .Configure<StockLedgerCoexistenceOptions>(
                configuration.GetSection(StockLedgerCoexistenceOptions.SectionName));

        services.AddScoped<
            IAdmissionQueueOperationalProjection,
            AdmissionQueueOperationalProjection>();
        services.AddScoped<
            IAdmissionQueueOperationRepo,
            AdmissionQueueOperationRepo>();
        services.AddScoped<IClinicQueueOperationDal, ClinicQueueOperationDal>();
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

        // Stock Ledger v2 — ports/repos not covered by Scrutor Nuna markers (S1-C1/C2/D1)
        services.AddScoped<ILegacyStockReadPort, LegacyStockReadPort>();
        services.AddScoped<ILegacyStockWriterPort, LegacyStockWriterPort>();
        services.AddScoped<IStockMutasiRepo, StockMutasiRepo>();
        services.AddScoped<IStockLegacyBindingRepo, StockLegacyBindingRepo>();
        services.AddScoped<IStockConsequenceUnitOfWork, StockConsequenceUnitOfWork>();
        services.AddScoped<LegacyScopeJournalReplayer>();
        services.AddScoped<LegacyFreshnessGate>();
        services.AddScoped<ILabExternalDal, LabExternalDal>();
        services.AddScoped<ILabExternalRepo, LabExternalRepo>();
        services.AddScoped<ILabDisciplineDal, LabDisciplineDal>();
        services.AddScoped<ILabDisciplineRepo, LabDisciplineRepo>();
        services.AddScoped<ILabTestGroupDal, LabTestGroupDal>();
        services.AddScoped<ILabTestGroupRepo, LabTestGroupRepo>();

        // PakaiBrgFeature (tb_trs_pakai, tb_trs_pakai2)
        services.AddScoped<IPakaiBrgDal, PakaiBrgDal>();
        services.AddScoped<IPakaiBrgItemDal, PakaiBrgItemDal>();
        services.AddScoped<IPakaiBrgRepo, PakaiBrgRepo>();

        // PenjualanFeature (tb_trs_dobill_umum3)
        services.AddScoped<IPenjualanItemBatchDal, PenjualanItemBatchDal>();

        // MutasiBrgFeature (BILRG_MutasiBrg, BILRG_MutasiBrgItem)
        services.AddInventoryContext();

        services.AddBrgFeature();
        services.AddScoped<IBrgHargaJualDal, BrgHargaJualDal>();
        services.AddTataRekeningFeature();
        services.AddScoped<ITrsDirectDal, TrsDirectDal>();
        services.AddScoped<Bilreg.Application.PaymentContext.TrsBillingFeature.ITrsDirectDal, TrsDirectDal>();
        services.AddScoped<IJurnalDal, JurnalDal>();
        services.AddScoped<Bilreg.Application.AccountingContext.JurnalFeature.IJurnalDal, JurnalDal>();
        services.AddScoped<Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg.IRegBiayaRepo, Bilreg.Infrastructure.PaymentContext.RegOutFeature.RegBiayaRepo>();
        services.AddScoped<Bilreg.Infrastructure.PaymentContext.RegOutFeature.IRegBiayaDal, Bilreg.Infrastructure.PaymentContext.RegOutFeature.RegBiayaDal>();

        services.AddScoped<IDeliveryOrderDal, DeliveryOrderDal>();
        services.AddScoped<IDeliveryOrderItemDal, DeliveryOrderItemDal>();
        services.AddScoped<IDeliveryInspectionDal, DeliveryInspectionDal>();
        services.AddScoped<IPoDeliveryTrackingDal, PoDeliveryTrackingDal>();
        services.AddScoped<IDeliveryValidationDal, DeliveryValidationDal>();
        services.AddScoped<IDeliveryOrderRepo, DeliveryOrderRepo>();
        services.AddPricingBarangFeature();
        services.AddApotekPenjualanFeature();
        services.AddApotekQueueFeature();

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
