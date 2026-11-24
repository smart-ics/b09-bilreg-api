using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.SalesContext.ResepFeature;
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
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueIntakeHandlerTest
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

    private readonly DateTime _now = new(2026, 10, 18, 9, 30, 0);

    public PharmacyQueueIntakeHandlerTest()
    {
        _clock.Setup(x => x.Now).Returns(_now);
    }

    private PharmacyQueueIntakeHandler CreateSut()
        => new(
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

    [Fact]
    public async Task Handle_Throws_When_RegId_NotFound()
    {
        _regRepo.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-INVALID")))
            .Returns(MayBe<RegModel>.None);

        var sut = CreateSut();
        var act = () => sut.Handle(new PharmacyQueueIntakeCmd(RegId: "REG-INVALID"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*REG-INVALID*");
    }

    [Fact]
    public async Task Handle_Throws_When_ServicePoint_NotFound()
    {
        _servicePoints
            .Setup(x => x.Get("SP-999"))
            .Returns(MayBe<PharmacyServicePointModel>.None);

        var sut = CreateSut();
        var act = () => sut.Handle(new PharmacyQueueIntakeCmd("SP-999"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SP-999*");
    }

    [Fact]
    public async Task Handle_Throws_When_ServicePoint_NotActive()
    {
        var servicePoint = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Resep Non-Racik", "A");
        servicePoint.Deactivate();

        _servicePoints
            .Setup(x => x.Get("SP-01"))
            .Returns(MayBe.From(servicePoint));

        var sut = CreateSut();
        var act = () => sut.Handle(new PharmacyQueueIntakeCmd("SP-01"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task Handle_Allocates_Sequence_OnDemand_And_Creates_Generic_And_Apotek_Entries()
    {
        // Arrange
        var servicePoint = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Resep Non-Racikan", "A");
        _servicePoints
            .Setup(x => x.Get("SP-01"))
            .Returns(MayBe.From(servicePoint));

        var businessDate = DateOnly.FromDateTime(_now);
        _antrianRepo.Setup(x => x.ListData(businessDate)).Returns([]);

        var queue = new AntrianModel(
            "ANT-001", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-01", "Resep Non-Racikan",
            new ServicePointType("SP-01", "Resep Non-Racikan"),
            [], _sequencer.Object);

        _antrianFactory
            .Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate))
            .Returns(queue);

        _sequencer
            .Setup(x => x.GetNextNoUrut("AN2610180000_SP-01"))
            .Returns(5);

        _queueEntries
            .Setup(x => x.ListByPhase("DEPO-RJ", ApotekPhaseEnum.TAKEN))
            .Returns([]);

        AntrianEntryModel? savedGeneric = null;
        _antrianRepo
            .Setup(x => x.SaveNewEntry(It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Callback<AntrianModel, AntrianEntryModel>((q, e) => savedGeneric = e);

        ApotekQueueEntryModel? savedApotek = null;
        _queueEntries
            .Setup(x => x.Save(It.IsAny<ApotekQueueEntryModel>()))
            .Callback<ApotekQueueEntryModel>(e => savedApotek = e);

        _publisher
            .Setup(x => x.PublishWorklistRefresh("DEPO-RJ", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new PharmacyQueueIntakeCmd("SP-01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AntrianId.Should().Be("ANT-001");
        result.NoUrut.Should().Be(5);
        result.QueueLabel.Should().Be("A005");
        result.ServicePointId.Should().Be("SP-01");
        result.ServicePointName.Should().Be("Resep Non-Racikan");
        result.QueuePrefix.Should().Be("A");
        result.ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
        result.TakenAt.Should().Be(_now);
        result.WaitingAheadCount.Should().Be(0);

        // Verify Generic Entry (Waiting = 0)
        savedGeneric.Should().NotBeNull();
        savedGeneric!.NoUrut.Should().Be(5);
        savedGeneric.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);
        savedGeneric.CreatedAt.Should().Be(_now);

        // Verify Apotek Entry (TAKEN, A005)
        savedApotek.Should().NotBeNull();
        savedApotek!.AntrianId.Should().Be("ANT-001");
        savedApotek.NoUrut.Should().Be(5);
        savedApotek.QueueLabel.Should().Be("A005");
        savedApotek.ServicePointId.Should().Be("SP-01");
        savedApotek.ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
        savedApotek.TakenAt.Should().Be(_now);

        // Verify SignalR broadcast
        _publisher.Verify(x => x.PublishWorklistRefresh("DEPO-RJ", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Calculates_WaitingAheadCount_Correctly()
    {
        var servicePoint = PharmacyServicePointModel.Create("SP-02", "DEPO-RJ", "Resep Racikan", "B");
        _servicePoints.Setup(x => x.Get("SP-02")).Returns(MayBe.From(servicePoint));

        var businessDate = DateOnly.FromDateTime(_now);
        var queue = new AntrianModel(
            "ANT-002", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-02", "Resep Racikan",
            new ServicePointType("SP-02", "Resep Racikan"),
            [], _sequencer.Object);

        _antrianFactory.Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate)).Returns(queue);
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(12);

        // 2 earlier tickets in TAKEN for this service point
        var ticket1 = ApotekQueueEntryModel.Create("ANT-002", 3, "SP-02", "B", _now.AddMinutes(-10));
        var ticket2 = ApotekQueueEntryModel.Create("ANT-002", 7, "SP-02", "B", _now.AddMinutes(-5));
        var otherSpTicket = ApotekQueueEntryModel.Create("ANT-001", 1, "SP-OTHER", "A", _now.AddMinutes(-8));

        _queueEntries
            .Setup(x => x.ListByPhase("DEPO-RJ", ApotekPhaseEnum.TAKEN))
            .Returns([ticket1, ticket2, otherSpTicket]);

        _publisher.Setup(x => x.PublishWorklistRefresh("DEPO-RJ", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.Handle(new PharmacyQueueIntakeCmd("SP-02"), CancellationToken.None);

        result.QueueLabel.Should().Be("B012");
        result.WaitingAheadCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Recovers_From_AdmissionQueueSessionRaceException()
    {
        var servicePoint = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Resep Non-Racikan", "A");
        _servicePoints.Setup(x => x.Get("SP-01")).Returns(MayBe.From(servicePoint));

        var businessDate = DateOnly.FromDateTime(_now);
        _antrianRepo.SetupSequence(x => x.ListData(businessDate))
            .Returns([]) // First check: not found
            .Returns([new AntrianHeaderView("ANT-WINNER", "Resep Non-Racikan", businessDate, TimeOnly.MinValue, "AN2610180000_SP-01")]); // Reload check

        var initialQueue = new AntrianModel("ANT-LOSER", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-01", "Resep Non-Racikan", new ServicePointType("SP-01", "Resep Non-Racikan"), [], _sequencer.Object);
        _antrianFactory.Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate)).Returns(initialQueue);

        var winnerQueue = new AntrianModel("ANT-WINNER", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-01", "Resep Non-Racikan", new ServicePointType("SP-01", "Resep Non-Racikan"), [], _sequencer.Object);
        _antrianRepo.Setup(x => x.LoadEntity(It.Is<IAntrianKey>(k => k.AntrianId == "ANT-WINNER")))
            .Returns(MayBe.From(winnerQueue));

        _sequencer.Setup(x => x.GetNextNoUrut("AN2610180000_SP-01")).Returns(3);

        var callCount = 0;
        _antrianRepo
            .Setup(x => x.SaveNewEntry(It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Callback<AntrianModel, AntrianEntryModel>((q, e) =>
            {
                callCount++;
                if (callCount == 1)
                    throw new AdmissionQueueSessionRaceException("AN2610180000_SP-01");
            });

        _queueEntries.Setup(x => x.ListByPhase(It.IsAny<string>(), ApotekPhaseEnum.TAKEN)).Returns([]);
        _publisher.Setup(x => x.PublishWorklistRefresh(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.Handle(new PharmacyQueueIntakeCmd("SP-01"), CancellationToken.None);

        result.AntrianId.Should().Be("ANT-WINNER");
        result.NoUrut.Should().Be(3);
        result.QueueLabel.Should().Be("A003");
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_RegId_Bpjs_Issues_PrefixA_And_Maps_Active_Prescriptions()
    {
        // Arrange
        var servicePointBpjs = PharmacyServicePointModel.Create("SP-FAR-BPJS", "DEPO-FAR-01", "Antrean Resep BPJS", "A");
        _servicePoints.Setup(x => x.Get("SP-FAR-BPJS")).Returns(MayBe.From(servicePointBpjs));

        var dokterReff = new DokterReff("D01", "dr. Budi, Sp.A");
        var layananReff = new LayananReff("L01", "Poli Anak");

        var regBpjs = new RegModel(
            "REG-BPJS-01",
            DateOnly.FromDateTime(_now),
            null!, null!, null!, null!,
            JenisRegEnum.RegJalan,
            new PasienReff("RM-001", "Budi Santoso", new DateOnly(1985, 5, 20), "L"),
            new TipeJaminanReff("1", "BPJS Kesehatan"),
            null!, null!, null!, null!,
            new PpaReff("D01", "dr. Budi, Sp.A"),
            layananReff,
            null!, null!, []);

        _regRepo.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-BPJS-01")))
            .Returns(MayBe.From(regBpjs));

        var resep1 = new ResepModel(
            "KP001",
            new RegReff("REG-BPJS-01", "RM-001", "Budi Santoso"),
            null!,
            dokterReff,
            layananReff,
            UrgenitasType.Load("1", "CITO"),
            new TipeBrgReff("TB01", "BPJS Fornas"),
            0, "Resep 1", null!, []);

        var resep2 = new ResepModel(
            "KP002",
            new RegReff("REG-BPJS-01", "RM-001", "Budi Santoso"),
            null!,
            dokterReff,
            layananReff,
            UrgenitasType.Load("0", "Normal"),
            new TipeBrgReff("TB01", "BPJS Fornas"),
            0, "Resep 2", null!, []);

        // voided resep should be excluded
        var resepVoid = new ResepModel(
            "KP003",
            new RegReff("REG-BPJS-01", "RM-001", "Budi Santoso"),
            null!,
            dokterReff,
            layananReff,
            UrgenitasType.Load("0", "Normal"),
            new TipeBrgReff("TB01", "BPJS Fornas"),
            0, "Resep 3", null!, []);
        resepVoid.Void("petugas1", "Salah input", _now);

        _resepRepo.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG-BPJS-01")))
            .Returns([resep1, resep2, resepVoid]);

        var businessDate = DateOnly.FromDateTime(_now);
        _antrianRepo.Setup(x => x.ListData(businessDate)).Returns([]);

        var queue = new AntrianModel(
            "ANT-BPJS-01", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-FAR-BPJS", "Antrean Resep BPJS",
            new ServicePointType("SP-FAR-BPJS", "Antrean Resep BPJS"),
            [], _sequencer.Object);

        _antrianFactory.Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate)).Returns(queue);
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(1);
        _queueEntries.Setup(x => x.ListByPhase("DEPO-FAR-01", ApotekPhaseEnum.TAKEN)).Returns([]);
        _publisher.Setup(x => x.PublishWorklistRefresh(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mappingRepo.Setup(x => x.GetByDemand(It.IsAny<QueueDemandKindEnum>(), It.IsAny<string>()))
            .Returns(MayBe<QueueMappingModel>.None);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new PharmacyQueueIntakeCmd(RegId: "REG-BPJS-01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ServicePointId.Should().Be("SP-FAR-BPJS");
        result.QueuePrefix.Should().Be("A");
        result.QueueLabel.Should().Be("A001");
        result.NoUrut.Should().Be(1);
        result.PatientName.Should().Be("Budi Santoso");
        result.MedicalRecordNo.Should().Be("RM-001");
        result.Penjamin.Should().Be("BPJS Kesehatan");

        // Verify auto-mapping for KP001 and KP002, but not for voided KP003
        _mappingRepo.Verify(x => x.Save(It.Is<QueueMappingModel>(m =>
            m.DemandId == "KP001" &&
            m.DemandKind == QueueDemandKindEnum.ResepKerja &&
            m.MappingMethod == QueueMappingMethodEnum.Tracker &&
            m.AntrianId == "ANT-BPJS-01" &&
            m.NoUrut == 1)), Times.Once);

        _mappingRepo.Verify(x => x.Save(It.Is<QueueMappingModel>(m =>
            m.DemandId == "KP002" &&
            m.DemandKind == QueueDemandKindEnum.ResepKerja &&
            m.MappingMethod == QueueMappingMethodEnum.Tracker &&
            m.AntrianId == "ANT-BPJS-01" &&
            m.NoUrut == 1)), Times.Once);

        _mappingRepo.Verify(x => x.Save(It.Is<QueueMappingModel>(m => m.DemandId == "KP003")), Times.Never);
    }

    [Fact]
    public async Task Handle_RegId_Umum_Allocates_SP_FAR_UMUM_PrefixB()
    {
        // Arrange
        var servicePointUmum = PharmacyServicePointModel.Create("SP-FAR-UMUM", "DEPO-FAR-01", "Antrean Obat Umum", "B");
        _servicePoints.Setup(x => x.Get("SP-FAR-UMUM")).Returns(MayBe.From(servicePointUmum));

        var regUmum = new RegModel(
            "REG-UMUM-01",
            DateOnly.FromDateTime(_now),
            null!, null!, null!, null!,
            JenisRegEnum.RegJalan,
            new PasienReff("RM-002", "Siti Aminah", new DateOnly(1995, 3, 12), "P"),
            new TipeJaminanReff("00000", "Bayar Sendiri"),
            null!, null!, null!, null!,
            new PpaReff("D02", "dr. Siti, Sp.PD"),
            new LayananReff("L02", "Poli Penyakit Dalam"),
            null!, null!, []);

        _regRepo.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-UMUM-01")))
            .Returns(MayBe.From(regUmum));

        _resepRepo.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG-UMUM-01")))
            .Returns([]);

        var businessDate = DateOnly.FromDateTime(_now);
        _antrianRepo.Setup(x => x.ListData(businessDate)).Returns([]);

        var queue = new AntrianModel(
            "ANT-UMUM-01", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-FAR-UMUM", "Antrean Obat Umum",
            new ServicePointType("SP-FAR-UMUM", "Antrean Obat Umum"),
            [], _sequencer.Object);

        _antrianFactory.Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate)).Returns(queue);
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(2);
        _queueEntries.Setup(x => x.ListByPhase("DEPO-FAR-01", ApotekPhaseEnum.TAKEN)).Returns([]);
        _publisher.Setup(x => x.PublishWorklistRefresh(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new PharmacyQueueIntakeCmd(RegId: "REG-UMUM-01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ServicePointId.Should().Be("SP-FAR-UMUM");
        result.QueuePrefix.Should().Be("B");
        result.QueueLabel.Should().Be("B002");
        result.NoUrut.Should().Be(2);
        result.PatientName.Should().Be("Siti Aminah");
        result.MedicalRecordNo.Should().Be("RM-002");
        result.Penjamin.Should().Be("Bayar Sendiri");
        _mappingRepo.Verify(x => x.Save(It.IsAny<QueueMappingModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Antrean_Umum_Without_RegId_Allocates_SP_FAR_UMUM_PrefixB()
    {
        // Arrange
        var servicePointUmum = PharmacyServicePointModel.Create("SP-FAR-UMUM", "DEPO-FAR-01", "Antrean Obat Umum", "B");
        _servicePoints.Setup(x => x.Get("SP-FAR-UMUM")).Returns(MayBe.From(servicePointUmum));

        var businessDate = DateOnly.FromDateTime(_now);
        _antrianRepo.Setup(x => x.ListData(businessDate)).Returns([]);

        var queue = new AntrianModel(
            "ANT-UMUM-02", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-FAR-UMUM", "Antrean Obat Umum",
            new ServicePointType("SP-FAR-UMUM", "Antrean Obat Umum"),
            [], _sequencer.Object);

        _antrianFactory.Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate)).Returns(queue);
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(3);
        _queueEntries.Setup(x => x.ListByPhase("DEPO-FAR-01", ApotekPhaseEnum.TAKEN)).Returns([]);
        _publisher.Setup(x => x.PublishWorklistRefresh(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act - Intake without RegId and without ServicePointId
        var result = await sut.Handle(new PharmacyQueueIntakeCmd(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ServicePointId.Should().Be("SP-FAR-UMUM");
        result.QueuePrefix.Should().Be("B");
        result.QueueLabel.Should().Be("B003");
        result.NoUrut.Should().Be(3);
        result.PatientName.Should().BeEmpty();
        _mappingRepo.Verify(x => x.Save(It.IsAny<QueueMappingModel>()), Times.Never);
    }
}
