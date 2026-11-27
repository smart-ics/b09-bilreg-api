using System.ComponentModel.DataAnnotations;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Application.RadContext.RadOrderFeature.Integration;
using Bilreg.Application.RadContext.RadOrderFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.RadContext.RadOrderFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

/// <summary>
/// Rangkaian pengujian integrasi skenario / siklus hidup penuh (lifecycle) untuk RadOrderFeature (P5-S11).
/// Menguji alur E2E in-memory / use case orchestration:
/// 1. Pembuatan Order dari EMR (RadOrderCreateFromEmrCmd) -> status Requested, format RDO*/RAD*, snapshot pasien, derived IsCito.
/// 2. Deteksi Duplikasi 24 Jam -> percobaan order ulang melempar DuplicateRadOrderWarningException (EX-06).
/// 3. Konfirmasi Duplikasi -> saat AllowDuplicateConfirmation == true, order baru berhasil dibuat.
/// 4. Idempotensi -> pengiriman ulang dengan EmrOrderId yang sama mengembalikan order eksisting tanpa duplikasi.
/// 5. Worklist Query (RadOrderWorklistQuery) -> verifikasi order muncul pada worklist sesuai filter tanggal dan modalitas.
/// 6. Detail Query (RadOrderGetQuery) -> verifikasi pengambilan detail order beserta seluruh item rincian.
/// 7. Pembatalan Order (RadOrderCancelCmd) -> mutasi status non-destructive ke Cancelled, pencatatan alasan, audit user/date.
/// 8. Guard Pembatalan -> pembatalan ulang pada order Cancelled ditolak (InvalidOperationException).
/// 9. Katalog Modalitas Lookup (RadTarifModalityLookupQuery) -> verifikasi pencarian katalog tindakan per modalitas.
/// 10. Integrated Master Scenario -> eksekusi beruntun seluruh siklus hidup pada satu runtime state terpadu.
/// 11. Skenario Pembebanan Biaya & Pembatalan (Order Charge & Cancel Lifecycle) -> transisi Requested -> Charged (BillingTindakanId) -> pembatalan memicu CancelCharge.
/// </summary>
public class RadOrderLifecycleScenarioTest
{
    #region In-Memory Test Harness

    private sealed class MutableTglJamProvider(DateTime initialNow) : ITglJamProvider
    {
        public DateTime Now { get; set; } = initialNow;
    }

    private sealed class RadOrderLifecycleHarness
    {
        public Dictionary<string, RadOrderModel> Orders { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> EmrOrderIndex { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<RadTarifModalityDto> TarifCatalog { get; } = [];
        public Dictionary<string, RegModel> Registrations { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, PpaType> Doctors { get; } = new(StringComparer.OrdinalIgnoreCase);

        public MutableTglJamProvider TimeProvider { get; }
        private int _orderIdSeq;
        private int _orderNoSeq;

        public Mock<ISequencer> SequencerMock { get; } = new();
        public Mock<IRadOrderRepo> RepoMock { get; } = new();
        public Mock<IRadOrderDal> OrderDalMock { get; } = new();
        public Mock<IRadOrderItemDal> ItemDalMock { get; } = new();
        public Mock<IRadTarifModalityDal> TarifDalMock { get; } = new();
        public Mock<IRegRepo> RegRepoMock { get; } = new();
        public Mock<IPpaRepo> PpaRepoMock { get; } = new();
        public Mock<IRadBillingIntegration> BillingMock { get; } = new();

        public RadItemResolutionService ResolutionService { get; }

        public RadOrderCreateFromEmrHandler CreateHandler { get; }
        public RadOrderChargeHandler ChargeHandler { get; }
        public RadOrderCancelHandler CancelHandler { get; }
        public RadOrderGetHandler GetHandler { get; }
        public RadOrderWorklistHandler WorklistHandler { get; }
        public RadTarifModalityLookupHandler LookupHandler { get; }

        public RadOrderLifecycleHarness(DateTime initialTime)
        {
            TimeProvider = new MutableTglJamProvider(initialTime);

            // Sequencer setup
            SequencerMock.Setup(s => s.GetNextNoUrut(RadOrderModel.IdPrefix))
                .Returns(() => ++_orderIdSeq);
            SequencerMock.Setup(s => s.GetNextNoUrut(RadOrderModel.OrderNoPrefix))
                .Returns(() => ++_orderNoSeq);

            // Repo setup (In-Memory aggregate persistence)
            RepoMock.Setup(r => r.SaveChanges(It.IsAny<RadOrderModel>()))
                .Callback<RadOrderModel>(model =>
                {
                    Orders[model.OrderId] = model;
                    if (!string.IsNullOrWhiteSpace(model.EmrOrderId))
                    {
                        EmrOrderIndex[model.EmrOrderId] = model.OrderId;
                    }
                });

            RepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
                .Returns<IRadOrderKey>(key =>
                {
                    if (key != null && Orders.TryGetValue(key.OrderId, out var model))
                        return MayBe.From(model);
                    return MayBe<RadOrderModel>.None;
                });

            RepoMock.Setup(r => r.LoadByEmrOrderId(It.IsAny<string>()))
                .Returns<string>(emrId =>
                {
                    if (!string.IsNullOrWhiteSpace(emrId) &&
                        EmrOrderIndex.TryGetValue(emrId, out var orderId) &&
                        Orders.TryGetValue(orderId, out var model))
                    {
                        return MayBe.From(model);
                    }
                    return MayBe<RadOrderModel>.None;
                });

            RepoMock.Setup(r => r.ListByRegId(It.IsAny<string>()))
                .Returns<string>(regId => Orders.Values.Where(o => o.Patient.RegId == regId));

            // Order DAL setup
            OrderDalMock.Setup(d => d.GetData(It.IsAny<IRadOrderKey>()))
                .Returns<IRadOrderKey>(key =>
                {
                    if (key != null && Orders.TryGetValue(key.OrderId, out var model))
                        return RadOrderDto.FromModel(model);
                    return null;
                });

            OrderDalMock.Setup(d => d.HasRecentOrder(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>()))
                .Returns<string, string, IEnumerable<string>, int>((patientId, modality, tarifIds, windowHours) =>
                {
                    var cutoff = TimeProvider.Now.AddHours(-windowHours);
                    var tarifSet = tarifIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

                    return Orders.Values.Any(o =>
                        string.Equals(o.Patient.PatientId, patientId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(o.Modality.ToString(), modality, StringComparison.OrdinalIgnoreCase) &&
                        o.RadOrderStatus != RadOrderStatusEnum.Cancelled &&
                        o.AuditTrail.Created.Timestamp >= cutoff &&
                        o.Items.Any(item => tarifSet.Contains(item.TarifId)));
                });

            OrderDalMock.Setup(d => d.GetWorklist(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns<DateTime, string, int?>((date, modality, status) =>
                {
                    return Orders.Values
                        .Where(o => o.AuditTrail.Created.Timestamp.Date == date.Date &&
                                    string.Equals(o.Modality.ToString(), modality, StringComparison.OrdinalIgnoreCase) &&
                                    (!status.HasValue || (int)o.RadOrderStatus == status.Value))
                        .Select(o => new RadOrderWorklistView(
                            o.OrderId,
                            o.OrderNo,
                            o.AuditTrail.Created.Timestamp,
                            o.Modality.ToString(),
                            (int)o.RadOrderStatus,
                            (int)o.Urgency,
                            o.Patient.PatientId,
                            o.Patient.PatientName,
                            o.DokterPengorderName,
                            o.LayananPengorderName,
                            o.Items.Count,
                            o.IsCito
                        ))
                        .ToList();
                });

            OrderDalMock.Setup(d => d.ListByRegId(It.IsAny<string>()))
                .Returns<string>(regId => Orders.Values.Where(o => o.Patient.RegId == regId).Select(RadOrderDto.FromModel));

            // Item DAL setup
            ItemDalMock.Setup(d => d.ListData(It.IsAny<IRadOrderKey>()))
                .Returns<IRadOrderKey>(key =>
                {
                    if (key != null && Orders.TryGetValue(key.OrderId, out var model))
                    {
                        return model.Items.Select(i => RadOrderItemDto.FromModel(model.OrderId, i)).ToList();
                    }
                    return [];
                });

            // Tarif Modality DAL setup
            TarifDalMock.Setup(t => t.GetData(It.IsAny<string>()))
                .Returns<string>(id => TarifCatalog.FirstOrDefault(x => string.Equals(x.TarifId, id, StringComparison.OrdinalIgnoreCase)));

            TarifDalMock.Setup(t => t.ListByModality(It.IsAny<string>()))
                .Returns<string>(mod => TarifCatalog.Where(x => string.Equals(x.Modality, mod, StringComparison.OrdinalIgnoreCase) && x.IsActive).ToList());

            TarifDalMock.Setup(t => t.ListActive())
                .Returns(() => TarifCatalog.Where(x => x.IsActive).ToList());

            // Reg & PPA Repo setups
            RegRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
                .Returns<IRegKey>(key =>
                {
                    if (key != null && Registrations.TryGetValue(key.RegId, out var reg))
                        return MayBe.From(reg);
                    return MayBe<RegModel>.None;
                });

            PpaRepoMock.Setup(p => p.LoadEntity(It.IsAny<IPpaKey>()))
                .Returns<IPpaKey>(key =>
                {
                    if (key != null && Doctors.TryGetValue(key.PpaId, out var ppa))
                        return MayBe.From(ppa);
                    return MayBe<PpaType>.None;
                });

            // Resolution service using real RadItemResolutionService
            ResolutionService = new RadItemResolutionService(TarifDalMock.Object);

            // Instantiate use cases handlers
            CreateHandler = new RadOrderCreateFromEmrHandler(
                RepoMock.Object,
                SequencerMock.Object,
                ResolutionService,
                TimeProvider,
                RegRepoMock.Object,
                PpaRepoMock.Object,
                OrderDalMock.Object);

            ChargeHandler = new RadOrderChargeHandler(
                RepoMock.Object,
                BillingMock.Object,
                TimeProvider);

            CancelHandler = new RadOrderCancelHandler(
                RepoMock.Object,
                BillingMock.Object,
                TimeProvider);

            BillingMock.Setup(b => b.ChargeOrder(It.IsAny<RadOrderModel>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns<RadOrderModel, string, DateTime>((order, user, dt) =>
                    order.OrderId.Length > 7 ? $"BILL_{order.OrderId[^7..]}" : $"BILL_{order.OrderId}");

            GetHandler = new RadOrderGetHandler(
                OrderDalMock.Object,
                ItemDalMock.Object);

            WorklistHandler = new RadOrderWorklistHandler(
                OrderDalMock.Object);

            LookupHandler = new RadTarifModalityLookupHandler(
                TarifDalMock.Object);

            SeedDefaultMasterData(initialTime);
        }

        private void SeedDefaultMasterData(DateTime initialTime)
        {
            // Seed Active Registration
            var pasienReff = new PasienReff("P0001", "Tn. Ahmad Dahlan", new DateOnly(1985, 5, 10), "L");
            var reg = new RegModel(
                "REG001",
                new DateOnly(initialTime.Year, initialTime.Month, initialTime.Day),
                new AuditInfoType("admisi_user", initialTime),
                AuditInfoType.Default,
                AuditInfoType.Default,
                AuditInfoType.Default,
                JenisRegEnum.RegJalan,
                pasienReff,
                TipeJaminanType.Default.ToReff(),
                PolisModel.Default.ToReff(),
                KelasType.Default.ToReff(),
                CaraMasukDkType.Default,
                RujukanType.Default.ToReff(),
                PpaType.Default.ToReff(),
                LayananType.Default.ToReff(),
                KarcisType.Default.ToReff(),
                new RegEligibilityType("-", "-", "-"),
                []);
            Registrations["REG001"] = reg;

            // Seed Doctor PPA
            var doctor = PpaType.Default with { PpaId = "DOK01", PpaName = "dr. Radiologi Spesialis, Sp.Rad" };
            Doctors["DOK01"] = doctor;

            // Seed Tarif Modality Catalog (CR and CT items)
            TarifCatalog.Add(new RadTarifModalityDto("TRF_CR01", "Thorax AP", "CR", true, "SYS", initialTime, "SYS", initialTime));
            TarifCatalog.Add(new RadTarifModalityDto("TRF_CR02", "Thorax Lateral", "CR", true, "SYS", initialTime, "SYS", initialTime));
            TarifCatalog.Add(new RadTarifModalityDto("TRF_CR03", "BNO Polos Abdomen", "CR", true, "SYS", initialTime, "SYS", initialTime));
            TarifCatalog.Add(new RadTarifModalityDto("TRF_CT01", "CT Scan Kepala Non-Kontras", "CT", true, "SYS", initialTime, "SYS", initialTime));
            TarifCatalog.Add(new RadTarifModalityDto("TRF_CT02", "CT Abdomen Kontras", "CT", true, "SYS", initialTime, "SYS", initialTime));
            TarifCatalog.Add(new RadTarifModalityDto("TRF_INACTIVE", "Tindakan Nonaktif", "CR", false, "SYS", initialTime, "SYS", initialTime));
        }
    }

    #endregion

    private readonly DateTime _baseTime = new(2026, 11, 24, 8, 30, 0);

    #region Requirement 1: Pembuatan Order dari EMR

    [Fact]
    public async Task Req1_CreateOrderFromEmr_GeneratesValidOrderWithSnapshotAndDerivedCito()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);
        var cmd = new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_01",
            EmrOrderId: "EMR-RAD-20261124-001",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items:
            [
                new RadOrderItemInputDto("TRF_CR01", (int)RadUrgencyEnum.Rutin),
                new RadOrderItemInputDto("TRF_CR02", (int)RadUrgencyEnum.Cito)
            ],
            DokterPengorderId: "DOK01",
            DokterPengorderName: "dr. Radiologi Spesialis, Sp.Rad",
            LayananPengorderId: "POLI_DALAM",
            LayananPengorderName: "Poli Penyakit Dalam",
            ClinicalNote: "Batuk berdarah, suspek pneumonia cito",
            Urgency: (int)RadUrgencyEnum.Rutin,
            AllowDuplicateConfirmation: false);

        // Act
        var response = await harness.CreateHandler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.OrderId.Should().StartWith("RDO");
        response.OrderNo.Should().StartWith("RAD");
        response.EmrOrderId.Should().Be("EMR-RAD-20261124-001");
        response.Modality.Should().Be("CR");
        response.RadOrderStatus.Should().Be((int)RadOrderStatusEnum.Requested);
        response.ItemCount.Should().Be(2);
        response.IsCito.Should().BeTrue("karena terdapat rincian item dengan urgensi Cito");

        // Verify stored aggregate
        var loaded = harness.Orders[response.OrderId];
        loaded.Should().NotBeNull();
        loaded.Patient.PatientId.Should().Be("P0001");
        loaded.Patient.PatientName.Should().Be("Tn. Ahmad Dahlan");
        loaded.Patient.BirthDate.Should().Be(new DateTime(1985, 5, 10));
        loaded.Patient.Gender.Should().Be("L");
        loaded.Patient.AgeAtOrder.Should().Be(41);
        loaded.DokterPengorderId.Should().Be("DOK01");
        loaded.LayananPengorderName.Should().Be("Poli Penyakit Dalam");
        loaded.Items.Should().HaveCount(2);
        loaded.Items.Should().ContainSingle(i => i.TarifId == "TRF_CR01" && i.Urgency == RadUrgencyEnum.Rutin && i.ItemNo == 1);
        loaded.Items.Should().ContainSingle(i => i.TarifId == "TRF_CR02" && i.Urgency == RadUrgencyEnum.Cito && i.ItemNo == 2);
    }

    #endregion

    #region Requirement 2: Deteksi Duplikasi 24 Jam

    [Fact]
    public async Task Req2_DuplicateOrderWithin24Hours_ThrowsDuplicateRadOrderWarningException()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);
        var initialCmd = new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_01",
            EmrOrderId: "EMR-RAD-001",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF_CR01", 0)],
            AllowDuplicateConfirmation: false);
        await harness.CreateHandler.Handle(initialCmd, CancellationToken.None);

        // Waktu bertambah 2 jam kemudian
        harness.TimeProvider.Now = _baseTime.AddHours(2);

        // Percobaan order ulang tindakan serupa untuk pasien yang sama tanpa konfirmasi
        var duplicateCmd = new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_02",
            EmrOrderId: "EMR-RAD-002",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF_CR01", 0)],
            AllowDuplicateConfirmation: false);

        // Act
        var act = () => harness.CreateHandler.Handle(duplicateCmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DuplicateRadOrderWarningException>();
        ex.Which.PatientId.Should().Be("P0001");
        ex.Which.Modality.Should().Be("CR");
        ex.Which.DuplicateTarifIds.Should().Contain("TRF_CR01");
        ex.Which.Message.Should().Contain("RAD_DUPLICATE_ORDER_WARNING");
        ex.Which.Message.Should().Contain("EX-06");
    }

    #endregion

    #region Requirement 3: Konfirmasi Duplikasi

    [Fact]
    public async Task Req3_DuplicateOrderWithConfirmation_SuccessfullyCreatesNewOrder()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);
        var initialCmd = new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_01",
            EmrOrderId: "EMR-RAD-001",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF_CR01", 0)],
            AllowDuplicateConfirmation: false);
        var initialResponse = await harness.CreateHandler.Handle(initialCmd, CancellationToken.None);

        harness.TimeProvider.Now = _baseTime.AddHours(3);

        // Order tindakan serupa dengan AllowDuplicateConfirmation == true
        var confirmedCmd = new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_02",
            EmrOrderId: "EMR-RAD-002",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF_CR01", 0)],
            AllowDuplicateConfirmation: true);

        // Act
        var confirmedResponse = await harness.CreateHandler.Handle(confirmedCmd, CancellationToken.None);

        // Assert
        confirmedResponse.Should().NotBeNull();
        confirmedResponse.OrderId.Should().NotBe(initialResponse.OrderId);
        confirmedResponse.OrderNo.Should().NotBe(initialResponse.OrderNo);
        confirmedResponse.EmrOrderId.Should().Be("EMR-RAD-002");
        confirmedResponse.RadOrderStatus.Should().Be((int)RadOrderStatusEnum.Requested);

        harness.Orders.Should().HaveCount(2);
        harness.Orders.ContainsKey(initialResponse.OrderId).Should().BeTrue();
        harness.Orders.ContainsKey(confirmedResponse.OrderId).Should().BeTrue();
    }

    #endregion

    #region Requirement 4: Idempotensi Pengiriman Ulang EMR

    [Fact]
    public async Task Req4_ResendingSameEmrOrderId_ReturnsExistingOrderWithoutDuplication()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);
        var cmd = new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_01",
            EmrOrderId: "EMR-RAD-IDEMPOTENT-001",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF_CR01", 0)],
            AllowDuplicateConfirmation: false);

        var firstResult = await harness.CreateHandler.Handle(cmd, CancellationToken.None);
        harness.Orders.Should().HaveCount(1);

        // Act: Kirim ulang request yang identik (misal akibat retry jaringan atau double submit)
        var secondResult = await harness.CreateHandler.Handle(cmd, CancellationToken.None);

        // Assert
        secondResult.Should().NotBeNull();
        secondResult.OrderId.Should().Be(firstResult.OrderId);
        secondResult.OrderNo.Should().Be(firstResult.OrderNo);
        secondResult.EmrOrderId.Should().Be(firstResult.EmrOrderId);
        secondResult.RadOrderStatus.Should().Be(firstResult.RadOrderStatus);

        harness.Orders.Should().HaveCount(1, "tidak boleh ada order duplikat di repositori");
    }

    #endregion

    #region Requirement 5: Worklist Query

    [Fact]
    public async Task Req5_WorklistQuery_ReturnsOrdersFilteredByDateAndModality()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);

        // Order 1: CR (Requested, Cito)
        await harness.CreateHandler.Handle(new RadOrderCreateFromEmrCmd(
            UserId: "DR01", EmrOrderId: "EMR-01", RegId: "REG001", PatientId: "P0001",
            PatientName: "Pasien CR 1", BirthDateYmd: "1985-05-10", Gender: "L",
            Modality: "CR", Items: [new RadOrderItemInputDto("TRF_CR01", (int)RadUrgencyEnum.Cito)]),
            CancellationToken.None);

        // Order 2: CR (Requested, Rutin)
        await harness.CreateHandler.Handle(new RadOrderCreateFromEmrCmd(
            UserId: "DR01", EmrOrderId: "EMR-02", RegId: "REG001", PatientId: "P0001",
            PatientName: "Pasien CR 2", BirthDateYmd: "1985-05-10", Gender: "L",
            Modality: "CR", Items: [new RadOrderItemInputDto("TRF_CR02", (int)RadUrgencyEnum.Rutin)],
            AllowDuplicateConfirmation: true),
            CancellationToken.None);

        // Order 3: CT (Requested)
        await harness.CreateHandler.Handle(new RadOrderCreateFromEmrCmd(
            UserId: "DR01", EmrOrderId: "EMR-03", RegId: "REG001", PatientId: "P0001",
            PatientName: "Pasien CT 1", BirthDateYmd: "1985-05-10", Gender: "L",
            Modality: "CT", Items: [new RadOrderItemInputDto("TRF_CT01", (int)RadUrgencyEnum.Rutin)]),
            CancellationToken.None);

        // Act: Kueri worklist modalitas CR pada tanggal hari ini
        var queryCr = new RadOrderWorklistQuery(_baseTime.Date, "CR", (int)RadOrderStatusEnum.Requested);
        var worklistCr = (await harness.WorklistHandler.Handle(queryCr, CancellationToken.None)).ToList();

        // Kueri worklist modalitas CT
        var queryCt = new RadOrderWorklistQuery(_baseTime.Date, "CT", (int)RadOrderStatusEnum.Requested);
        var worklistCt = (await harness.WorklistHandler.Handle(queryCt, CancellationToken.None)).ToList();

        // Assert
        worklistCr.Should().HaveCount(2);
        worklistCr.Should().AllSatisfy(x => x.Modality.Should().Be("CR"));
        worklistCr.Should().ContainSingle(x => x.IsCito);
        worklistCr.Should().ContainSingle(x => !x.IsCito);

        worklistCt.Should().HaveCount(1);
        worklistCt[0].Modality.Should().Be("CT");
    }

    #endregion

    #region Requirement 6: Detail Query

    [Fact]
    public async Task Req6_DetailQuery_ReturnsCompleteHeaderAndItems()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);
        var createCmd = new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_01",
            EmrOrderId: "EMR-RAD-DET-001",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items:
            [
                new RadOrderItemInputDto("TRF_CR01", (int)RadUrgencyEnum.Rutin),
                new RadOrderItemInputDto("TRF_CR02", (int)RadUrgencyEnum.Cito)
            ],
            DokterPengorderId: "DOK01",
            DokterPengorderName: "dr. Radiologi Spesialis, Sp.Rad",
            LayananPengorderId: "POLI_DALAM",
            LayananPengorderName: "Poli Penyakit Dalam",
            ClinicalNote: "Evaluasi efusi pleura");

        var created = await harness.CreateHandler.Handle(createCmd, CancellationToken.None);

        // Act
        var query = new RadOrderGetQuery(created.OrderId);
        var detail = await harness.GetHandler.Handle(query, CancellationToken.None);

        // Assert
        detail.Should().NotBeNull();
        detail!.Header.Should().NotBeNull();
        detail.Header.OrderId.Should().Be(created.OrderId);
        detail.Header.OrderNo.Should().Be(created.OrderNo);
        detail.Header.PatientName.Should().Be("Tn. Ahmad Dahlan");
        detail.Header.Modality.Should().Be("CR");
        detail.Header.DokterPengorderName.Should().Be("dr. Radiologi Spesialis, Sp.Rad");
        detail.Header.ClinicalNote.Should().Be("Evaluasi efusi pleura");

        detail.Items.Should().HaveCount(2);
        detail.Items.Should().ContainSingle(i => i.ItemNo == 1 && i.TarifId == "TRF_CR01" && i.TarifName == "Thorax AP");
        detail.Items.Should().ContainSingle(i => i.ItemNo == 2 && i.TarifId == "TRF_CR02" && i.TarifName == "Thorax Lateral");
    }

    #endregion

    #region Requirement 7: Pembatalan Order (Non-Destructive & Audit)

    [Fact]
    public async Task Req7_CancelOrder_MutatesStatusNonDestructively_AndAuditsCancellation()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);
        var created = await harness.CreateHandler.Handle(new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_01",
            EmrOrderId: "EMR-RAD-CANCEL-001",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF_CR01", 0)]),
            CancellationToken.None);

        // Majukan waktu 1 jam saat pembatalan
        var cancelTime = _baseTime.AddHours(1);
        harness.TimeProvider.Now = cancelTime;

        var cancelCmd = new RadOrderCancelCmd(
            OrderId: created.OrderId,
            UserId: "DR_CANCEL_01",
            Reason: "Pasien menolak pemeriksaan karena sesak berat");

        // Act
        await harness.CancelHandler.Handle(cancelCmd, CancellationToken.None);

        // Assert
        var order = harness.Orders[created.OrderId];
        order.Should().NotBeNull("data order tetap utuh (non-destructive mutation)");
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be("Pasien menolak pemeriksaan karena sesak berat");
        order.CancelledUserId.Should().Be("DR_CANCEL_01");
        order.CancelledDate.Should().Be(cancelTime);
        order.IsCancelled.Should().BeTrue();
        order.IsActive.Should().BeFalse();
        order.AuditTrail.Modified.UserId.Should().Be("DR_CANCEL_01");
        order.AuditTrail.Modified.Timestamp.Should().Be(cancelTime);
    }

    #endregion

    #region Requirement 8: Guard Pembatalan (Status Bukan Requested/Scheduled Ditolak)

    [Fact]
    public async Task Req8_CancellingAlreadyCancelledOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);
        var created = await harness.CreateHandler.Handle(new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_01",
            EmrOrderId: "EMR-RAD-GUARD-001",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF_CR01", 0)]),
            CancellationToken.None);

        // Pembatalan pertama sukses
        await harness.CancelHandler.Handle(
            new RadOrderCancelCmd(created.OrderId, "DR_01", "Pembatalan pertama yang sah"),
            CancellationToken.None);

        // Act: Percobaan pembatalan kedua pada order yang sudah berstatus Cancelled
        var secondCancelCmd = new RadOrderCancelCmd(
            created.OrderId,
            "DR_02",
            "Mencoba membatalkan lagi order yang sudah Cancelled");

        var act = () => harness.CancelHandler.Handle(secondCancelCmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("Cancelled");
        ex.Which.Message.Should().Contain("pembatalan hanya diizinkan untuk status Requested, Scheduled, atau Charged");
    }

    #endregion

    #region Requirement 9: Katalog Modalitas Lookup

    [Fact]
    public async Task Req9_TarifModalityLookup_ReturnsCatalogFilteredByModality()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);

        // Act
        var crCatalog = (await harness.LookupHandler.Handle(new RadTarifModalityLookupQuery("CR"), CancellationToken.None)).ToList();
        var ctCatalog = (await harness.LookupHandler.Handle(new RadTarifModalityLookupQuery("CT"), CancellationToken.None)).ToList();
        var allCatalog = (await harness.LookupHandler.Handle(new RadTarifModalityLookupQuery(null), CancellationToken.None)).ToList();

        // Assert
        crCatalog.Should().HaveCount(3);
        crCatalog.Should().AllSatisfy(x =>
        {
            x.Modality.Should().Be("CR");
            x.IsActive.Should().BeTrue();
        });
        crCatalog.Should().NotContain(x => x.TarifId == "TRF_INACTIVE", "item tidak aktif harus diabaikan");

        ctCatalog.Should().HaveCount(2);
        ctCatalog.Should().AllSatisfy(x => x.Modality.Should().Be("CT"));

        allCatalog.Should().HaveCount(5);
        allCatalog.Should().NotContain(x => x.TarifId == "TRF_INACTIVE");
    }

    #endregion

    #region Requirement 10: Skenario Siklus Hidup Penuh End-to-End

    [Fact]
    public async Task Req10_CompleteEndToEndLifecycle_ExecutesIntegratedFlowSeamlessly()
    {
        // 1. Arrange & Inisialisasi Harness
        var harness = new RadOrderLifecycleHarness(_baseTime);

        // 2. Lookup Katalog Modalitas: DPJP memeriksa tindakan CR yang tersedia di radiologi
        var lookupResult = (await harness.LookupHandler.Handle(new RadTarifModalityLookupQuery("CR"), CancellationToken.None)).ToList();
        lookupResult.Should().NotBeEmpty();
        lookupResult.Select(x => x.TarifId).Should().Contain(["TRF_CR01", "TRF_CR02"]);

        // 3. Pembuatan Order dari EMR (Alur Utama)
        var createCmd1 = new RadOrderCreateFromEmrCmd(
            UserId: "DR_DPJP_01",
            EmrOrderId: "EMR-JOURNEY-001",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items:
            [
                new RadOrderItemInputDto("TRF_CR01", (int)RadUrgencyEnum.Rutin),
                new RadOrderItemInputDto("TRF_CR02", (int)RadUrgencyEnum.Cito)
            ],
            DokterPengorderId: "DOK01",
            DokterPengorderName: "dr. Radiologi Spesialis, Sp.Rad",
            LayananPengorderId: "POLI_DALAM",
            LayananPengorderName: "Poli Penyakit Dalam",
            ClinicalNote: "Pneumonia akut berat, batuk 2 minggu",
            AllowDuplicateConfirmation: false);

        var order1 = await harness.CreateHandler.Handle(createCmd1, CancellationToken.None);
        order1.OrderId.Should().StartWith("RDO");
        order1.OrderNo.Should().StartWith("RAD");
        order1.RadOrderStatus.Should().Be((int)RadOrderStatusEnum.Requested);
        order1.IsCito.Should().BeTrue();

        // 4. Deteksi Duplikasi 24 Jam: DPJP lain mencoba order tindakan TRF_CR01 lagi tanpa konfirmasi
        harness.TimeProvider.Now = _baseTime.AddHours(2);
        var dupCmdWithoutConfirm = new RadOrderCreateFromEmrCmd(
            UserId: "DR_DPJP_02",
            EmrOrderId: "EMR-JOURNEY-002",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF_CR01", 0)],
            AllowDuplicateConfirmation: false);

        var dupAct = () => harness.CreateHandler.Handle(dupCmdWithoutConfirm, CancellationToken.None);
        await dupAct.Should().ThrowAsync<DuplicateRadOrderWarningException>();

        // 5. Konfirmasi Duplikasi: DPJP mengonfirmasi order tindakan ulang memang diperlukan
        var dupCmdWithConfirm = dupCmdWithoutConfirm with { AllowDuplicateConfirmation = true };
        var order2 = await harness.CreateHandler.Handle(dupCmdWithConfirm, CancellationToken.None);
        order2.OrderId.Should().NotBe(order1.OrderId);
        order2.OrderNo.Should().NotBe(order1.OrderNo);
        order2.RadOrderStatus.Should().Be((int)RadOrderStatusEnum.Requested);

        // 6. Idempotensi: Pengiriman ulang order 1 dengan EmrOrderId identik tidak membuat order baru
        var retryOrder1 = await harness.CreateHandler.Handle(createCmd1, CancellationToken.None);
        retryOrder1.OrderId.Should().Be(order1.OrderId);
        harness.Orders.Should().HaveCount(2, "jumlah order tetap 2 karena order 1 bersifat idempoten");

        // 7. Worklist Radiologi: Petugas radiologi memantau antrean modalitas CR
        var worklistQuery = new RadOrderWorklistQuery(_baseTime.Date, "CR", (int)RadOrderStatusEnum.Requested);
        var worklist = (await harness.WorklistHandler.Handle(worklistQuery, CancellationToken.None)).ToList();
        worklist.Should().HaveCount(2);
        worklist.Select(w => w.OrderId).Should().Contain([order1.OrderId, order2.OrderId]);

        // 8. Detail Query: Radiografer membuka rincian order 1 sebelum persiapan pemeriksaan
        var detailQuery = new RadOrderGetQuery(order1.OrderId);
        var detail = await harness.GetHandler.Handle(detailQuery, CancellationToken.None);
        detail.Should().NotBeNull();
        detail!.Header.OrderId.Should().Be(order1.OrderId);
        detail.Header.OrderNo.Should().Be(order1.OrderNo);
        detail.Items.Should().HaveCount(2);

        // 9. Pembatalan Order: Pasien memutuskan pulang paksa dan menolak pemeriksaan
        harness.TimeProvider.Now = _baseTime.AddHours(4);
        var cancelCmd = new RadOrderCancelCmd(
            OrderId: order1.OrderId,
            UserId: "DR_DPJP_01",
            Reason: "Pasien pulang paksa dan menolak seluruh tindakan penunjang");

        await harness.CancelHandler.Handle(cancelCmd, CancellationToken.None);

        // Verifikasi mutasi non-destructive
        var cancelledOrder = harness.Orders[order1.OrderId];
        cancelledOrder.RadOrderStatus.Should().Be(RadOrderStatusEnum.Cancelled);
        cancelledOrder.CancelledReason.Should().Be("Pasien pulang paksa dan menolak seluruh tindakan penunjang");
        cancelledOrder.CancelledUserId.Should().Be("DR_DPJP_01");
        cancelledOrder.IsActive.Should().BeFalse();

        // 10. Guard Pembatalan: Percobaan pembatalan ulang ditolak
        var cancelAgainAct = () => harness.CancelHandler.Handle(cancelCmd, CancellationToken.None);
        await cancelAgainAct.Should().ThrowAsync<InvalidOperationException>();

        // 11. Worklist Pasca Pembatalan: Order 1 tidak lagi muncul dalam antrean Requested
        var activeWorklist = (await harness.WorklistHandler.Handle(worklistQuery, CancellationToken.None)).ToList();
        activeWorklist.Should().HaveCount(1);
        activeWorklist[0].OrderId.Should().Be(order2.OrderId);
    }

    #endregion

    #region Requirement 11: Order Charge & Cancellation Lifecycle

    [Fact]
    public async Task Req11_OrderChargeAndCancellationLifecycle_TransitionsToChargedWithBillingTindakanId_AndTriggersCancelCharge()
    {
        // 1. Arrange & Inisialisasi Harness
        var harness = new RadOrderLifecycleHarness(_baseTime);
        var createCmd = new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_01",
            EmrOrderId: "EMR-RAD-CHARGE-001",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items:
            [
                new RadOrderItemInputDto("TRF_CR01", (int)RadUrgencyEnum.Rutin),
                new RadOrderItemInputDto("TRF_CR02", (int)RadUrgencyEnum.Cito)
            ],
            DokterPengorderId: "DOK01",
            DokterPengorderName: "dr. Radiologi Spesialis, Sp.Rad",
            LayananPengorderId: "POLI_DALAM",
            LayananPengorderName: "Poli Penyakit Dalam",
            ClinicalNote: "Pemeriksaan sebelum tindakan");

        // 2. Order created -> status Requested
        var created = await harness.CreateHandler.Handle(createCmd, CancellationToken.None);
        created.RadOrderStatus.Should().Be((int)RadOrderStatusEnum.Requested);

        var initialOrder = harness.Orders[created.OrderId];
        initialOrder.BillingTindakanId.Should().BeEmpty();
        initialOrder.BillingLastError.Should().BeEmpty();

        // 3. Order charge triggered via RadOrderChargeCmd -> transitions to Charged with BillingTindakanId
        var chargeTime = _baseTime.AddMinutes(15);
        harness.TimeProvider.Now = chargeTime;

        var chargeCmd = new RadOrderChargeCmd(created.OrderId, "BILLING_STAFF_01");
        var chargeResponse = await harness.ChargeHandler.Handle(chargeCmd, CancellationToken.None);

        chargeResponse.Should().NotBeNull();
        chargeResponse.Success.Should().BeTrue();
        chargeResponse.BillingTindakanId.Should().NotBeNullOrWhiteSpace();
        chargeResponse.BillingLastError.Should().BeNull();

        var chargedOrder = harness.Orders[created.OrderId];
        chargedOrder.RadOrderStatus.Should().Be(RadOrderStatusEnum.Charged);
        chargedOrder.BillingTindakanId.Should().Be(chargeResponse.BillingTindakanId);
        chargedOrder.BillingLastError.Should().BeEmpty();

        // 4. Order cancellation attempted while Charged -> triggers CancelCharge
        var cancelTime = _baseTime.AddHours(1);
        harness.TimeProvider.Now = cancelTime;

        var cancelCmd = new RadOrderCancelCmd(
            created.OrderId,
            "DR_CANCEL_01",
            "Pasien menolak pemeriksaan setelah billing dibukukan");

        await harness.CancelHandler.Handle(cancelCmd, CancellationToken.None);

        // Verify CancelCharge was called on billing integration
        harness.BillingMock.Verify(b => b.CancelCharge(
            created.OrderId,
            "REG001",
            "DR_CANCEL_01",
            "Pasien menolak pemeriksaan setelah billing dibukukan",
            cancelTime), Times.Once);

        // Verify aggregate is cancelled
        var cancelledOrder = harness.Orders[created.OrderId];
        cancelledOrder.RadOrderStatus.Should().Be(RadOrderStatusEnum.Cancelled);
        cancelledOrder.CancelledReason.Should().Be("Pasien menolak pemeriksaan setelah billing dibukukan");
        cancelledOrder.CancelledUserId.Should().Be("DR_CANCEL_01");
        cancelledOrder.CancelledDate.Should().Be(cancelTime);
        cancelledOrder.IsCancelled.Should().BeTrue();
        cancelledOrder.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Req11_ChargedOrder_WhenBillingRejectsCancellationDueToFinalizedAccount_OrderRemainsCharged()
    {
        // Arrange
        var harness = new RadOrderLifecycleHarness(_baseTime);
        var createCmd = new RadOrderCreateFromEmrCmd(
            UserId: "DR_EMR_01",
            EmrOrderId: "EMR-RAD-CHARGE-002",
            RegId: "REG001",
            PatientId: "P0001",
            PatientName: "Tn. Ahmad Dahlan",
            BirthDateYmd: "1985-05-10",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF_CR01", 0)]);

        var created = await harness.CreateHandler.Handle(createCmd, CancellationToken.None);
        await harness.ChargeHandler.Handle(new RadOrderChargeCmd(created.OrderId, "BILLING_STAFF"), CancellationToken.None);

        // Setup billing integration to throw exception because cashier account is finalized / paid
        harness.BillingMock.Setup(b => b.CancelCharge(
                created.OrderId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .Throws(new InvalidOperationException("Rekening pasien sudah final atau lunas di kasir"));

        // Act
        var cancelCmd = new RadOrderCancelCmd(created.OrderId, "DR_01", "Pembatalan order yang sudah dibayar di kasir");
        var act = () => harness.CancelHandler.Handle(cancelCmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("kasir");

        var order = harness.Orders[created.OrderId];
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Charged);
        order.IsCancelled.Should().BeFalse();
    }

    #endregion
}
