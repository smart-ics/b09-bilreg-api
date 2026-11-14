using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueDemandMappingTest
{
    private readonly InMemoryQueueMappingRepo _mappingRepo = new();
    private readonly Mock<IApotekQueueEntryRepo> _queueEntryRepo = new();
    private readonly Mock<IPharmacyServicePointRepo> _servicePointRepo = new();
    private readonly InMemoryResepRepo _resepRepo = new();
    private readonly Mock<IPharmacyQueuePublisher> _publisher = new();
    private readonly AllowAllAuth _auth = new();
    private readonly Mock<ITelaahResepRepo> _telaahRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ISalesOrderRepo> _salesOrderRepo = new();
    private readonly Mock<ITrackerPharmacyPort> _trackerPort = new();

    private readonly PharmacyQueueMapDemandHandler _mapHandler;
    private readonly PharmacyQueueFlagPasienHadirHandler _flagHandler;

    public PharmacyQueueDemandMappingTest()
    {
        _mapHandler = new PharmacyQueueMapDemandHandler(
            _mappingRepo,
            _queueEntryRepo.Object,
            _servicePointRepo.Object,
            _resepRepo,
            _publisher.Object,
            _auth,
            _telaahRepo.Object,
            _regRepo.Object,
            _mediator.Object,
            _salesOrderRepo.Object,
            _trackerPort.Object);

        _flagHandler = new PharmacyQueueFlagPasienHadirHandler(
            _mappingRepo,
            _queueEntryRepo.Object,
            _servicePointRepo.Object,
            _publisher.Object,
            _auth);
    }

    [Fact]
    public async Task MapDemand_SinglePrescription_MapsSuccessfullyAndPublishesSignalR()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-001", 1, "SP01", "A");
        _queueEntryRepo.Setup(x => x.Get("ANT-001", 1)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", "DEP-RAJAL", "Layanan A", "A");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        SeedResep("KP101");

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: "ANT-001",
            NoUrut: 1,
            DemandKind: QueueDemandKindEnum.Resep,
            DemandId: "KP101",
            UserId: "officer1");

        // Act
        var resp = await _mapHandler.Handle(cmd, default);

        // Assert
        resp.Success.Should().BeTrue();
        resp.AntrianId.Should().Be("ANT-001");
        resp.NoUrut.Should().Be(1);
        resp.DemandId.Should().Be("KP101");

        var saved = _mappingRepo.GetByDemand(QueueDemandKindEnum.Resep, "KP101");
        saved.HasValue.Should().BeTrue();
        saved.Value.AntrianId.Should().Be("ANT-001");
        saved.Value.NoUrut.Should().Be(1);

        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-RAJAL", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MapDemand_MultiplePrescriptions_MapsAllSuccessfully()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-002", 2, "SP01", "A");
        _queueEntryRepo.Setup(x => x.Get("ANT-002", 2)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", "DEP-RAJAL", "Layanan A", "A");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        SeedResep("KP201");
        SeedResep("KP202");

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: "ANT-002",
            NoUrut: 2,
            DemandKind: QueueDemandKindEnum.Resep,
            DemandId: "KP201",
            DemandIds: ["KP201", "KP202"],
            UserId: "officer1");

        // Act
        var resp = await _mapHandler.Handle(cmd, default);

        // Assert
        resp.Success.Should().BeTrue();

        var map1 = _mappingRepo.GetByDemand(QueueDemandKindEnum.Resep, "KP201");
        map1.HasValue.Should().BeTrue();
        map1.Value.AntrianId.Should().Be("ANT-002");
        map1.Value.NoUrut.Should().Be(2);

        var map2 = _mappingRepo.GetByDemand(QueueDemandKindEnum.Resep, "KP202");
        map2.HasValue.Should().BeTrue();
        map2.Value.AntrianId.Should().Be("ANT-002");
        map2.Value.NoUrut.Should().Be(2);

        var allMapped = _mappingRepo.ListByQueue("ANT-002", 2);
        allMapped.Should().HaveCount(2);
    }

    [Fact]
    public async Task MapDemand_QueueTicketNotFound_ThrowsApotekDomainException()
    {
        // Arrange
        _queueEntryRepo.Setup(x => x.Get("ANT-NON-EXISTENT", 99)).Returns(MayBe<ApotekQueueEntryModel>.None);

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: "ANT-NON-EXISTENT",
            NoUrut: 99,
            DemandKind: QueueDemandKindEnum.Resep,
            DemandId: "KP101",
            UserId: "officer1");

        // Act
        var act = () => _mapHandler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Antrean 'ANT-NON-EXISTENT-99' tidak ditemukan*");
    }

    [Fact]
    public async Task MapDemand_PrescriptionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-001", 1, "SP01", "A");
        _queueEntryRepo.Setup(x => x.Get("ANT-001", 1)).Returns(MayBe.From(entry));

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: "ANT-001",
            NoUrut: 1,
            DemandKind: QueueDemandKindEnum.Resep,
            DemandId: "KP_NOT_EXIST",
            UserId: "officer1");

        // Act
        var act = () => _mapHandler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Resep 'KP_NOT_EXIST' tidak ditemukan*");
    }

    [Fact]
    public async Task FlagPasienHadir_ValidQueue_SetsPasienHadirTrueAndPublishesSignalR()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-003", 3, "SP01", "A");
        _queueEntryRepo.Setup(x => x.Get("ANT-003", 3)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", "DEP-RAJAL", "Layanan A", "A");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        SeedResep("KP301");

        // Petakan tiket ke resep terlebih dahulu
        var map = QueueMappingModel.Create(
            QueueDemandKindEnum.Resep,
            "KP301",
            "ANT-003",
            3,
            "",
            QueueMappingMethodEnum.Manual,
            "officer1",
            DateTime.Now,
            pasienHadir: false);
        _mappingRepo.SaveChanges(map);

        var cmd = new PharmacyQueueFlagPasienHadirCmd(
            DemandKind: QueueDemandKindEnum.Resep,
            DemandId: "KP301",
            AntrianId: "ANT-003",
            NoUrut: 3,
            UserId: "officer1");

        // Act
        var resp = await _flagHandler.Handle(cmd, default);

        // Assert
        resp.Success.Should().BeTrue();
        resp.PasienHadir.Should().BeTrue();

        var updated = _mappingRepo.GetByDemand(QueueDemandKindEnum.Resep, "KP301");
        updated.HasValue.Should().BeTrue();
        updated.Value.PasienHadir.Should().BeTrue();
        updated.Value.PasienHadirAt.Should().NotBe(ApotekDate.Empty);

        // Memastikan notifikasi prioritas disiarkan ke worklist Telaah Resep
        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-RAJAL", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FlagPasienHadir_MultipleMappedPrescriptions_MarksAllPasienHadir()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-004", 4, "SP01", "A");
        _queueEntryRepo.Setup(x => x.Get("ANT-004", 4)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", "DEP-RAJAL", "Layanan A", "A");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        SeedResep("KP401");
        SeedResep("KP402");

        var map1 = QueueMappingModel.Create(QueueDemandKindEnum.Resep, "KP401", "ANT-004", 4, "", QueueMappingMethodEnum.Manual, "officer", DateTime.Now);
        var map2 = QueueMappingModel.Create(QueueDemandKindEnum.Resep, "KP402", "ANT-004", 4, "", QueueMappingMethodEnum.Manual, "officer", DateTime.Now);
        _mappingRepo.SaveChanges(map1);
        _mappingRepo.SaveChanges(map2);

        var cmd = new PharmacyQueueFlagPasienHadirCmd(
            DemandKind: QueueDemandKindEnum.Resep,
            DemandId: "KP401",
            AntrianId: "ANT-004",
            NoUrut: 4,
            UserId: "officer1");

        // Act
        var resp = await _flagHandler.Handle(cmd, default);

        // Assert
        resp.Success.Should().BeTrue();

        var updated1 = _mappingRepo.GetByDemand(QueueDemandKindEnum.Resep, "KP401").Value;
        var updated2 = _mappingRepo.GetByDemand(QueueDemandKindEnum.Resep, "KP402").Value;

        updated1.PasienHadir.Should().BeTrue();
        updated2.PasienHadir.Should().BeTrue();
    }

    [Fact]
    public async Task FlagPasienHadir_QueueTicketNotFound_ThrowsApotekDomainException()
    {
        // Arrange
        _queueEntryRepo.Setup(x => x.Get("ANT-GHOST", 999)).Returns(MayBe<ApotekQueueEntryModel>.None);

        var cmd = new PharmacyQueueFlagPasienHadirCmd(
            DemandKind: QueueDemandKindEnum.Resep,
            DemandId: "KP501",
            AntrianId: "ANT-GHOST",
            NoUrut: 999,
            UserId: "officer1");

        // Act
        var act = () => _flagHandler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Antrean 'ANT-GHOST-999' tidak ditemukan*");
    }

    [Fact]
    public async Task MapDemand_AutomaticallyMarksPasienHadirTrueAndSetsTimestamp()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-AUTO", 1, "SP01", "A");
        _queueEntryRepo.Setup(x => x.Get("ANT-AUTO", 1)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", "DEP-RAJAL", "Layanan A", "A");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        SeedResep("KP_AUTO_01");

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: "ANT-AUTO",
            NoUrut: 1,
            DemandKind: QueueDemandKindEnum.Resep,
            DemandId: "KP_AUTO_01",
            UserId: "officer1");

        // Act
        var resp = await _mapHandler.Handle(cmd, default);

        // Assert
        resp.Success.Should().BeTrue();
        var mapping = _mappingRepo.GetByDemand(QueueDemandKindEnum.Resep, "KP_AUTO_01");
        mapping.HasValue.Should().BeTrue();
        mapping.Value.PasienHadir.Should().BeTrue();
        mapping.Value.PasienHadirAt.Should().NotBe(ApotekDate.Empty);
    }

    [Fact]
    public async Task MapDemand_WhenEntryRegIdEmpty_BackfillsRegIdAndPasienIdFromResep()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-BACKFILL", 5, "SP01", "B");
        entry.RegId.Should().BeEmpty();
        entry.PasienId.Should().BeEmpty();
        _queueEntryRepo.Setup(x => x.Get("ANT-BACKFILL", 5)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", "DEP-RAJAL", "Layanan Umum", "B");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        SeedResep("KP_BACKFILL_01", regId: "REG_BF_99", pasienId: "PSN_BF_99");

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: "ANT-BACKFILL",
            NoUrut: 5,
            DemandKind: QueueDemandKindEnum.ResepKerja,
            DemandId: "KP_BACKFILL_01",
            UserId: "officer1");

        // Act
        var resp = await _mapHandler.Handle(cmd, default);

        // Assert
        resp.Success.Should().BeTrue();
        entry.RegId.Should().Be("REG_BF_99");
        entry.PasienId.Should().Be("PSN_BF_99");
        _queueEntryRepo.Verify(x => x.SaveChanges(entry), Times.Once);
    }

    [Fact]
    public async Task MapDemand_WhenEntryPhaseIsTakenOrDilewati_TransitionsToPenerimaanAndCallsTrackerServeOnce()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-SERVE", 10, "SP01", "A");
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
        _queueEntryRepo.Setup(x => x.Get("ANT-SERVE", 10)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", "DEP-RAJAL", "Layanan A", "A");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        SeedResep("KP_SERVE_01");

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: "ANT-SERVE",
            NoUrut: 10,
            DemandKind: QueueDemandKindEnum.ResepKerja,
            DemandId: "KP_SERVE_01",
            PasienTrackerId: "TRK_S10",
            UserId: "officer1");

        // Act
        var resp = await _mapHandler.Handle(cmd, default);

        // Assert
        resp.Success.Should().BeTrue();
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.PENERIMAAN);
        _trackerPort.Verify(x => x.ServeOnce("ANT-SERVE", 10, "TRK_S10", "KP_SERVE_01", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task MapDemand_WhenTelaahApprovedAndBpjs_TriggersAutoBpjsPenjualanAndTransitionsToPenyiapan()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-BPJS-AUTO", 12, "SP01", "A");
        _queueEntryRepo.Setup(x => x.Get("ANT-BPJS-AUTO", 12)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", "DEP-RAJAL", "Layanan BPJS", "A");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        SeedResep("KP_BPJS_01", regId: "REG_BPJS_01", pasienId: "PSN_BPJS_01", tipeBrgId: "JKN00", tipeBrgName: "BPJS");

        var item = new TelaahResepItemModel(
            itemNo: 1,
            resepItemNo: 1,
            disposition: TelaahDispositionEnum.AcceptedAsPrescribed,
            acceptedBrgId: "BRG01",
            acceptedBrgName: "Test Drug",
            acceptedQty: 10m,
            reason: "",
            pharmacistId: "APT01");
        var telaah = TelaahResepModel.Open("KP_BPJS_01", "REG_BPJS_01", [item]);
        telaah.Start("APT01", DateTime.Now);
        telaah.Complete(DateTime.Now); // Approved -> CanEstablishSalesOrder == true

        _telaahRepo.Setup(x => x.LoadByResep("KP_BPJS_01")).Returns(MayBe.From(telaah));

        _mediator.Setup(x => x.Send(It.Is<PenjualanCreateFromTelaahCmd>(c => c.ResepId == "KP_BPJS_01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "DU_BPJS_01" });

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: "ANT-BPJS-AUTO",
            NoUrut: 12,
            DemandKind: QueueDemandKindEnum.ResepKerja,
            DemandId: "KP_BPJS_01",
            UserId: "officer1");

        // Act
        var resp = await _mapHandler.Handle(cmd, default);

        // Assert
        resp.Success.Should().BeTrue();
        _mediator.Verify(x => x.Send(It.Is<PenjualanCreateFromTelaahCmd>(c => c.ResepId == "KP_BPJS_01"), It.IsAny<CancellationToken>()), Times.Once);
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.PENYIAPAN);
    }

    [Fact]
    public async Task MapDemand_WhenTelaahNotApprovedOrNonBpjs_KeepsEntryInPenerimaan()
    {
        // Arrange
        var entry = ApotekQueueEntryModel.Create("ANT-UMUM-AUTO", 15, "SP02", "B");
        _queueEntryRepo.Setup(x => x.Get("ANT-UMUM-AUTO", 15)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP02", "DEP-RAJAL", "Layanan Mandiri", "B");
        _servicePointRepo.Setup(x => x.Get("SP02")).Returns(MayBe.From(sp));

        SeedResep("KP_UMUM_01", regId: "REG_UMUM_01", pasienId: "PSN_UMUM_01", tipeBrgId: "00000", tipeBrgName: "Umum");

        var item = new TelaahResepItemModel(
            itemNo: 1,
            resepItemNo: 1,
            disposition: TelaahDispositionEnum.AcceptedAsPrescribed,
            acceptedBrgId: "BRG01",
            acceptedBrgName: "Test Drug",
            acceptedQty: 10m,
            reason: "",
            pharmacistId: "APT01");
        var telaah = TelaahResepModel.Open("KP_UMUM_01", "REG_UMUM_01", [item]);
        telaah.Start("APT01", DateTime.Now);
        telaah.Complete(DateTime.Now); // Approved, but Non-BPJS!

        _telaahRepo.Setup(x => x.LoadByResep("KP_UMUM_01")).Returns(MayBe.From(telaah));

        var cmd = new PharmacyQueueMapDemandCmd(
            AntrianId: "ANT-UMUM-AUTO",
            NoUrut: 15,
            DemandKind: QueueDemandKindEnum.ResepKerja,
            DemandId: "KP_UMUM_01",
            UserId: "officer1");

        // Act
        var resp = await _mapHandler.Handle(cmd, default);

        // Assert
        resp.Success.Should().BeTrue();
        _mediator.Verify(x => x.Send(It.IsAny<PenjualanCreateFromTelaahCmd>(), It.IsAny<CancellationToken>()), Times.Never);
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.PENERIMAAN);
    }

    #region Helpers

    private void SeedResep(
        string resepId,
        string regId = "REG01",
        string pasienId = "PSN01",
        string tipeBrgId = "00000",
        string tipeBrgName = "Umum")
    {
        var regReff = new RegReff(regId, pasienId, "Pasien Test");
        var resep = new ResepModel(
            resepId: resepId,
            reg: regReff,
            bodyMetric: BodyMetricType.Default(),
            dokter: DokterType.Default.ToReff(),
            layanan: LayananType.Default.ToReff(),
            layananStok: LayananType.Default.ToReff(),
            urgenitas: UrgenitasType.Default,
            tipeBrg: new TipeBrgReff(tipeBrgId, tipeBrgName),
            iter: 0,
            description: "Test Prescription",
            tglRencanaPeriksa: DateTime.Today,
            resepAsalReffId: AppConst.DASH,
            voidReason: AppConst.DASH,
            auditTrail: AuditTrailType.Create("doc", DateTime.Now),
            listObat: []);

        var brg = BrgObatType.Default with { BrgId = "BRG01", BrgName = "Test Drug" };
        resep.AddObat(brg, SatuanType.Default, 10m, 0, EtiketType.Default);

        _resepRepo.SaveChanges(resep);
    }

    #endregion
}
