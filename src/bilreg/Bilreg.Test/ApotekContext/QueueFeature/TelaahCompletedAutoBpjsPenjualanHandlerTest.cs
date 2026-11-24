using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.DispensingFeature.UseCases;
using Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class TelaahCompletedAutoBpjsPenjualanHandlerTest
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly InMemoryQueueMappingRepo _mappingRepo = new();
    private readonly Mock<IApotekQueueEntryRepo> _queueEntryRepo = new();
    private readonly Mock<IPharmacyServicePointRepo> _servicePointRepo = new();
    private readonly InMemoryResepRepo _resepRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly InMemorySalesOrderRepo _salesOrderRepo = new();
    private readonly Mock<IPharmacyQueuePublisher> _publisher = new();

    private readonly TelaahCompletedAutoBpjsPenjualanHandler _sut;

    public TelaahCompletedAutoBpjsPenjualanHandlerTest()
    {
        _sut = new TelaahCompletedAutoBpjsPenjualanHandler(
            _mediator.Object,
            _mappingRepo,
            _queueEntryRepo.Object,
            _servicePointRepo.Object,
            _resepRepo,
            _regRepo.Object,
            _salesOrderRepo,
            _publisher.Object);
    }

    [Fact]
    public async Task Handle_BpjsPatient_PasienHadirTrue_ExecutesPenjualan_AndDispensing_AndTransitionsToPenyiapan()
    {
        // Arrange
        const string resepId = "KP-BPJS-01";
        const string regId = "REG-BPJS-01";
        const string antrianId = "ANT-BPJS";
        const int noUrut = 10;
        const string depoId = "DEP-RAJAL";

        // Seed Resep & Reg BPJS
        SeedResepAndReg(resepId, regId, tipeJaminanId: "JKN00", tipeJaminanName: "BPJS PBI");

        // Seed ServicePoint & QueueEntry
        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP01", "A");
        entry.StartServicePenerimaan(DateTime.Now); // Phase = PENERIMAAN
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", depoId, "Layanan Non-Racik", "A");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        // Map queue ticket to resep with PasienHadir = true
        var mapping = QueueMappingModel.Create(
            QueueDemandKindEnum.Resep,
            resepId,
            antrianId,
            noUrut,
            "",
            QueueMappingMethodEnum.Manual,
            "officer1",
            DateTime.Now,
            pasienHadir: true,
            pasienHadirAt: DateTime.Now);
        _mappingRepo.SaveChanges(mapping);

        // Setup Mediator Mock for PenjualanCreateFromTelaahCmd & SalesOrder / Dispensing
        _mediator.Setup(x => x.Send(It.Is<PenjualanCreateFromTelaahCmd>(c => c.ResepId == resepId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "DU240001" });

        // Seed SalesOrder
        var so = SalesOrderModel.Rehydrate(
            "SO-01",
            SalesOrderSourceKindEnum.ResepKerja,
            resepId,
            "ATR-01",
            regId,
            "MR01",
            "Pasien BPJS",
            PayerPathEnum.Bpjs,
            PartialReasonEnum.None,
            SalesOrderStatusEnum.Established,
            SalesOrderResolvedReasonEnum.None,
            1,
            [SalesOrderItemModel.Establish(1, 1, "BRG01", "Amoxicillin", "TAB", 10m, FornasCoverageEnum.Covered, "DU240001", false)],
            [],
            []);
        _salesOrderRepo.SaveChanges(so);

        _mediator.Setup(x => x.Send(It.Is<DispensingEstablishCmd>(c => c.SalesOrderId == "SO-01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DispensingResponse("DSP-01", DispensingStatusEnum.Established, 1));

        var notification = new TelaahCompletedNotification(
            ResepId: resepId,
            TelaahResepId: "ATR-01",
            TelaahStatus: TelaahStatusEnum.Approved,
            UserId: "apoteker1",
            CompletedAt: DateTime.Now);

        // Act
        await _sut.Handle(notification, default);

        // Assert:
        // 1. PenjualanCreateFromTelaahCmd terpanggil
        _mediator.Verify(x => x.Send(It.Is<PenjualanCreateFromTelaahCmd>(c => c.ResepId == resepId), It.IsAny<CancellationToken>()), Times.Once);

        // 2. DispensingEstablishCmd terpanggil
        _mediator.Verify(x => x.Send(It.Is<DispensingEstablishCmd>(c => c.SalesOrderId == "SO-01" && c.PenjualanId == "DU240001"), It.IsAny<CancellationToken>()), Times.Once);

        // 3. QueueEntry berpindah fase ke PENYIAPAN
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.PENYIAPAN);
        _queueEntryRepo.Verify(x => x.SaveChanges(It.Is<ApotekQueueEntryModel>(e => e.ApotekPhase == ApotekPhaseEnum.PENYIAPAN)), Times.Once);

        // 4. SignalR disiarkan ke depo
        _publisher.Verify(x => x.PublishWorklistRefresh(depoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BpjsPatient_PasienHadirFalse_DoesNotExecuteAutomation()
    {
        // Arrange
        const string resepId = "KP-BPJS-02";
        const string regId = "REG-BPJS-02";
        const string antrianId = "ANT-BPJS2";
        const int noUrut = 11;

        SeedResepAndReg(resepId, regId, tipeJaminanId: "JKN00", tipeJaminanName: "BPJS");

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP01", "A");
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        // PasienHadir = false (Pasien belum tiba di loket farmasi)
        var mapping = QueueMappingModel.Create(
            QueueDemandKindEnum.Resep,
            resepId,
            antrianId,
            noUrut,
            "",
            QueueMappingMethodEnum.Manual,
            "officer1",
            DateTime.Now,
            pasienHadir: false);
        _mappingRepo.SaveChanges(mapping);

        var notification = new TelaahCompletedNotification(
            ResepId: resepId,
            TelaahResepId: "ATR-02",
            TelaahStatus: TelaahStatusEnum.Approved,
            UserId: "apoteker1",
            CompletedAt: DateTime.Now);

        // Act
        await _sut.Handle(notification, default);

        // Assert: Tidak boleh otomatis menerbitkan penjualan karena pasien belum hadir fisik
        _mediator.Verify(x => x.Send(It.IsAny<PenjualanCreateFromTelaahCmd>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(x => x.Send(It.IsAny<DispensingEstablishCmd>(), It.IsAny<CancellationToken>()), Times.Never);
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
    }

    [Fact]
    public async Task Handle_GeneralPatient_PasienHadirTrue_DoesNotExecuteAutoPenjualan_PublishesRefreshForSiapPenjualanTab()
    {
        // Arrange
        const string resepId = "KP-UMUM-01";
        const string regId = "REG-UMUM-01";
        const string antrianId = "ANT-UMUM";
        const int noUrut = 12;
        const string depoId = "DEP-RAJAL";

        // Pasien UMUM (00000)
        SeedResepAndReg(resepId, regId, tipeJaminanId: "00000", tipeJaminanName: "Umum / Bayar Sendiri");

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP01", "A");
        entry.StartServicePenerimaan(DateTime.Now);
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP01", depoId, "Layanan A", "A");
        _servicePointRepo.Setup(x => x.Get("SP01")).Returns(MayBe.From(sp));

        var mapping = QueueMappingModel.Create(
            QueueDemandKindEnum.Resep,
            resepId,
            antrianId,
            noUrut,
            "",
            QueueMappingMethodEnum.Manual,
            "officer1",
            DateTime.Now,
            pasienHadir: true);
        _mappingRepo.SaveChanges(mapping);

        var notification = new TelaahCompletedNotification(
            ResepId: resepId,
            TelaahResepId: "ATR-03",
            TelaahStatus: TelaahStatusEnum.Approved,
            UserId: "apoteker1",
            CompletedAt: DateTime.Now);

        // Act
        await _sut.Handle(notification, default);

        // Assert:
        // 1. Penjualan otomatis TIDAK dijalankan (karena pasien umum butuh konfirmasi biaya & bayar kasir)
        _mediator.Verify(x => x.Send(It.IsAny<PenjualanCreateFromTelaahCmd>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(x => x.Send(It.IsAny<DispensingEstablishCmd>(), It.IsAny<CancellationToken>()), Times.Never);

        // 2. Fase tiket tetap pada PENERIMAAN (siap dipanggil ulang di Loket 1 pada tab 'Siap Penjualan')
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.PENERIMAAN);

        // 3. SignalR refresh tetap disiarkan ke depo
        _publisher.Verify(x => x.PublishWorklistRefresh(depoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnmappedPrescription_DoesNothing()
    {
        // Arrange
        const string resepId = "KP-UNMAPPED";
        SeedResepAndReg(resepId, "REG-UNMAPPED", "JKN00", "BPJS");

        var notification = new TelaahCompletedNotification(
            ResepId: resepId,
            TelaahResepId: "ATR-04",
            TelaahStatus: TelaahStatusEnum.Approved,
            UserId: "apoteker1",
            CompletedAt: DateTime.Now);

        // Act
        await _sut.Handle(notification, default);

        // Assert
        _mediator.Verify(x => x.Send(It.IsAny<PenjualanCreateFromTelaahCmd>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(x => x.PublishWorklistRefresh(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectedTelaahStatus_DoesNothing()
    {
        // Arrange
        const string resepId = "KP-REJECTED";
        SeedResepAndReg(resepId, "REG-REJECTED", "JKN00", "BPJS");

        var notification = new TelaahCompletedNotification(
            ResepId: resepId,
            TelaahResepId: "ATR-05",
            TelaahStatus: TelaahStatusEnum.Rejected,
            UserId: "apoteker1",
            CompletedAt: DateTime.Now);

        // Act
        await _sut.Handle(notification, default);

        // Assert
        _mediator.Verify(x => x.Send(It.IsAny<PenjualanCreateFromTelaahCmd>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #region Helpers

    private void SeedResepAndReg(string resepId, string regId, string tipeJaminanId, string tipeJaminanName)
    {
        var reg = new RegModel(
            regId,
            DateOnly.FromDateTime(DateTime.Today),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff("MR01", "Pasien BPJS", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "L"),
            new TipeJaminanReff(tipeJaminanId, tipeJaminanName),
            PolisModel.Default.ToReff(),
            new KelasReff("KLS1", "Kelas 1"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);

        _regRepo.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(reg));

        var regReff = new RegReff(regId, "MR01", "Pasien BPJS");
        var resep = new ResepModel(
            resepId: resepId,
            reg: regReff,
            bodyMetric: BodyMetricType.Default(),
            dokter: DokterType.Default.ToReff(),
            layanan: LayananType.Default.ToReff(),
            layananStok: LayananType.Default.ToReff(),
            urgenitas: UrgenitasType.Default,
            tipeBrg: TipeBrgType.Default.ToReff(),
            iter: 0,
            description: "BPJS/Umum Prescription",
            tglRencanaPeriksa: DateTime.Today,
            resepAsalReffId: AppConst.DASH,
            voidReason: AppConst.DASH,
            auditTrail: AuditTrailType.Create("doc", DateTime.Now),
            listObat: []);

        var brg = BrgObatType.Default with { BrgId = "BRG01", BrgName = "Amoxicillin" };
        resep.AddObat(brg, SatuanType.Default, 10m, 0, EtiketType.Default);

        _resepRepo.SaveChanges(resep);
    }

    #endregion
}
