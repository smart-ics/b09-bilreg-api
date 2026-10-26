using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueEndToEndSimulationTest
{
    private readonly Mock<IPharmacyServicePointRepo> _servicePoints = new();
    private readonly Mock<IApotekQueueEntryRepo> _queueEntries = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IAntrianFactory> _antrianFactory = new();
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly Mock<IPharmacyQueuePublisher> _publisher = new();
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly AllowAllAuth _auth = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IResepRepo> _resepRepo = new();
    private readonly Mock<IPharmacyQueueMappingRepo> _mappingRepo = new();
    private readonly Mock<IPharmacyLoketServicePointRepo> _loketServicePoints = new();
    private readonly Mock<IPharmacyQueueOperationRepo> _operations = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();

    private readonly DateTime _now = new(2026, 10, 26, 8, 30, 0);
    private readonly byte[] _tokenV1 = [1, 0, 0, 0, 0, 0, 0, 1];
    private readonly byte[] _tokenV2 = [2, 0, 0, 0, 0, 0, 0, 2];

    public PharmacyQueueEndToEndSimulationTest()
    {
        _clock.Setup(x => x.Now).Returns(_now);
    }

    [Fact]
    public async Task EndToEnd_Pharmacy_Queue_Lifecycle_Simulation_Succeeds()
    {
        // =========================================================================
        // SETUP: Service Points in Depot DEPO-FAR-01
        // SP-FAR-BPJS (Prefix A, Antrean Resep BPJS)
        // SP-FAR-UMUM (Prefix B, Antrean Obat Umum)
        // =========================================================================
        var spBpjs = PharmacyServicePointModel.Create("SP-FAR-BPJS", "DEPO-FAR-01", "Antrean Resep BPJS", "A");
        var spUmum = PharmacyServicePointModel.Create("SP-FAR-UMUM", "DEPO-FAR-01", "Antrean Obat Umum", "B");

        _servicePoints.Setup(x => x.Get("SP-FAR-BPJS")).Returns(MayBe.From(spBpjs));
        _servicePoints.Setup(x => x.Get("SP-FAR-UMUM")).Returns(MayBe.From(spUmum));

        var businessDate = DateOnly.FromDateTime(_now);
        _antrianRepo.Setup(x => x.ListData(businessDate)).Returns([]);

        var queueBpjs = new AntrianModel(
            "ANT-BPJS-01", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610260000_SP-FAR-BPJS", "Antrean Resep BPJS",
            new ServicePointType("SP-FAR-BPJS", "Antrean Resep BPJS"),
            [], _sequencer.Object);

        var queueUmum = new AntrianModel(
            "ANT-UMUM-01", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610260000_SP-FAR-UMUM", "Antrean Obat Umum",
            new ServicePointType("SP-FAR-UMUM", "Antrean Obat Umum"),
            [], _sequencer.Object);

        _antrianFactory.Setup(x => x.Create(It.Is<ServicePointType>(s => s.ServicePointCode == "SP-FAR-BPJS"), businessDate))
            .Returns(queueBpjs);
        _antrianFactory.Setup(x => x.Create(It.Is<ServicePointType>(s => s.ServicePointCode == "SP-FAR-UMUM"), businessDate))
            .Returns(queueUmum);

        _publisher.Setup(x => x.PublishWorklistRefresh(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publisher.Setup(x => x.PublishDisplayRefresh(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var inMemoryQueueEntries = new List<ApotekQueueEntryModel>();
        _queueEntries.Setup(x => x.Save(It.IsAny<ApotekQueueEntryModel>()))
            .Callback<ApotekQueueEntryModel>(e => inMemoryQueueEntries.Add(e));
        _queueEntries.Setup(x => x.ListByPhase("DEPO-FAR-01", ApotekPhaseEnum.TAKEN))
            .Returns(() => inMemoryQueueEntries.Where(e => e.ApotekPhase == ApotekPhaseEnum.TAKEN).ToList());

        var inMemoryMappings = new List<QueueMappingModel>();
        _mappingRepo.Setup(x => x.Save(It.IsAny<QueueMappingModel>()))
            .Callback<QueueMappingModel>(m => inMemoryMappings.Add(m));
        _mappingRepo.Setup(x => x.GetByDemand(It.IsAny<QueueDemandKindEnum>(), It.IsAny<string>()))
            .Returns(MayBe<QueueMappingModel>.None);
        _mappingRepo.Setup(x => x.ListByQueue(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((antrianId, noUrut) =>
                inMemoryMappings.Where(m => m.AntrianId == antrianId && m.NoUrut == noUrut).ToList());

        // =========================================================================
        // STEP 1: Pasien scan barcode RegId BPJS di Kiosk
        // -> Tiket terbit dengan Prefix A + e-resep terpetakan otomatis
        // =========================================================================
        var dokter = new DokterReff("D01", "dr. Budi, Sp.A");
        var poliAnak = new LayananReff("L01", "Poli Anak");
        var regBpjs = new RegModel(
            "REG-BPJS-2026-001",
            DateOnly.FromDateTime(_now),
            null!, null!, null!, null!,
            JenisRegEnum.RegJalan,
            new PasienReff("RM-001", "Budi Santoso", new DateOnly(1985, 5, 20), "L"),
            new TipeJaminanReff("1", "BPJS Kesehatan"),
            null!, null!, null!, null!,
            new PpaReff("D01", "dr. Budi, Sp.A"),
            poliAnak,
            null!, null!, []);

        _regRepo.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-BPJS-2026-001")))
            .Returns(MayBe.From(regBpjs));

        var resepBpjs1 = new ResepModel(
            "KP-BPJS-991",
            new RegReff("REG-BPJS-2026-001", "RM-001", "Budi Santoso"),
            null!, dokter, poliAnak,
            UrgenitasType.Load("1", "CITO"),
            new TipeBrgReff("TB01", "BPJS Fornas"),
            0, "Resep Fornas 1", null!, []);

        var resepBpjs2 = new ResepModel(
            "KP-BPJS-992",
            new RegReff("REG-BPJS-2026-001", "RM-001", "Budi Santoso"),
            null!, dokter, poliAnak,
            UrgenitasType.Load("0", "Normal"),
            new TipeBrgReff("TB01", "BPJS Fornas"),
            0, "Resep Fornas 2", null!, []);

        _resepRepo.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG-BPJS-2026-001")))
            .Returns([resepBpjs1, resepBpjs2]);

        _sequencer.Setup(x => x.GetNextNoUrut("AN2610260000_SP-FAR-BPJS")).Returns(1);

        var intakeHandler = new PharmacyQueueIntakeHandler(
            _servicePoints.Object,
            _queueEntries.Object,
            _antrianRepo.Object,
            _antrianFactory.Object,
            _sequencer.Object,
            _publisher.Object,
            _clock.Object,
            _auth,
            _regRepo.Object,
            _resepRepo.Object,
            _mappingRepo.Object);

        var ticketBpjs = await intakeHandler.Handle(
            new PharmacyQueueIntakeCmd(RegId: "REG-BPJS-2026-001"),
            CancellationToken.None);

        // Verification Step 1:
        ticketBpjs.QueuePrefix.Should().Be("A");
        ticketBpjs.QueueLabel.Should().Be("A001");
        ticketBpjs.NoUrut.Should().Be(1);
        ticketBpjs.PatientName.Should().Be("Budi Santoso");
        ticketBpjs.Penjamin.Should().Be("BPJS Kesehatan");

        inMemoryMappings.Should().HaveCount(2);
        inMemoryMappings.Select(m => m.DemandId).Should().Contain(["KP-BPJS-991", "KP-BPJS-992"]);

        // =========================================================================
        // STEP 2: Pasien tekan tombol "Antrean Umum" di Kiosk
        // -> Tiket terbit dengan Prefix B tanpa RegId & tanpa auto-mapping
        // =========================================================================
        _sequencer.Setup(x => x.GetNextNoUrut("AN2610260000_SP-FAR-UMUM")).Returns(2);

        var ticketUmum = await intakeHandler.Handle(
            new PharmacyQueueIntakeCmd(), // Antrean Umum
            CancellationToken.None);

        // Verification Step 2:
        ticketUmum.QueuePrefix.Should().Be("B");
        ticketUmum.QueueLabel.Should().Be("B002");
        ticketUmum.NoUrut.Should().Be(2);
        ticketUmum.ServicePointId.Should().Be("SP-FAR-UMUM");
        ticketUmum.PatientName.Should().BeEmpty();

        // Mappings count remains 2 (no mapping added for Umum OTC)
        inMemoryMappings.Should().HaveCount(2);

        // =========================================================================
        // STEP 3: Admin mengubah setting Loket 1 menjadi "Hanya BPJS" di LoketActiveCallBar
        // -> Loket 1 (APT-RCV-01) hanya melayani SP-FAR-BPJS
        // =========================================================================
        var idleLoket1 = new PharmacyLoketSession(
            LoketKey: "APT-RCV-01",
            LoketType: LoketTypeEnum.PenerimaanResep,
            DisplayName: "Loket Penerimaan 1",
            DepoFarmasiId: "DEPO-FAR-01",
            AntrianId: "",
            NoUrut: 0,
            ClaimState: LoketClaimStateEnum.Idle,
            IsActive: false,
            AnnouncementVersion: 0,
            CalledAt: ApotekDate.Empty,
            ServiceStartedAt: ApotekDate.Empty,
            ReleasedAt: ApotekDate.Empty,
            RowVersion: _tokenV1);

        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(idleLoket1));

        var assignedServicePoints = new List<string> { "SP-FAR-BPJS", "SP-FAR-UMUM" };
        _loketServicePoints.Setup(x => x.ListServicePointsByLoket("APT-RCV-01"))
            .Returns(() => assignedServicePoints);
        _loketServicePoints.Setup(x => x.ReplaceServicePoints("APT-RCV-01", It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Callback<string, IEnumerable<string>, string, DateTime>((loket, sps, user, at) =>
            {
                assignedServicePoints.Clear();
                assignedServicePoints.AddRange(sps);
            });

        var adminUpdateHandler = new UpdateLoketServicePointsCmdHandler(
            _loketServicePoints.Object,
            _operations.Object,
            _publisher.Object,
            _auditRepo.Object,
            _clock.Object,
            _auth);

        var updateResponse = await adminUpdateHandler.Handle(
            new UpdateLoketServicePointsCmd("APT-RCV-01", ["SP-FAR-BPJS"], _tokenV1),
            CancellationToken.None);

        // Verification Step 3:
        updateResponse.LoketKey.Should().Be("APT-RCV-01");
        updateResponse.ServicePointIds.Should().BeEquivalentTo(["SP-FAR-BPJS"]);
        assignedServicePoints.Should().BeEquivalentTo(["SP-FAR-BPJS"]);

        // =========================================================================
        // STEP 4: Tiket dipanggil di Loket 1 -> Loket 1 hanya memanggil tiket Prefix A,
        // e-resep langsung terbuka di Workbench, dan layar TV display memanggil nomor
        // =========================================================================
        _operations.Setup(x => x.TryCallLoket("APT-RCV-01", "ANT-BPJS-01", 1, _tokenV1, "petugas-loket1", _now))
            .Returns(true);

        var calledLoket1 = idleLoket1 with
        {
            AntrianId = "ANT-BPJS-01",
            NoUrut = 1,
            ClaimState = LoketClaimStateEnum.Called,
            IsActive = true,
            AnnouncementVersion = 1,
            CalledAt = _now,
            RowVersion = _tokenV2
        };

        _operations.SetupSequence(x => x.GetLoket("APT-RCV-01"))
            .Returns(MayBe.From(idleLoket1))
            .Returns(MayBe.From(calledLoket1));

        var callHandler = new PharmacyQueueCallPenerimaanCmdHandler(
            _operations.Object,
            _queueEntries.Object,
            _publisher.Object,
            _auditRepo.Object,
            _clock.Object,
            _auth,
            _loketServicePoints.Object);

        var callResult = await callHandler.Handle(
            new PharmacyQueueCallPenerimaanCmd("APT-RCV-01", null, null, null, "petugas-loket1"),
            CancellationToken.None);

        // Verification Step 4:
        callResult.Success.Should().BeTrue();
        callResult.AntrianId.Should().Be("ANT-BPJS-01");
        callResult.NoUrut.Should().Be(1);
        callResult.QueueLabel.Should().Be("A001");

        // Verify mapped prescriptions associated with called ticket
        var activeDemandMappings = inMemoryMappings
            .Where(m => m.AntrianId == callResult.AntrianId && m.NoUrut == callResult.NoUrut)
            .Select(m => m.DemandId)
            .ToList();

        activeDemandMappings.Should().Contain(["KP-BPJS-991", "KP-BPJS-992"]);

        // Verify SignalR display broadcast for TV display room
        _publisher.Verify(x => x.PublishDisplayRefresh("APT-RCV-01", 1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
