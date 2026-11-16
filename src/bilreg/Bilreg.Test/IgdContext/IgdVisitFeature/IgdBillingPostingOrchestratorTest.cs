using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature;

public class IgdBillingPostingOrchestratorTest
{
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();
    private readonly Mock<IJaminanRepo> _jaminanRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<ITindakanRepo> _tindakanRepoMock = new();
    private readonly Mock<ITindakanIgdRepo> _tindakanIgdRepoMock = new();
    private readonly Mock<IBhpIgdRepo> _bhpIgdRepoMock = new();
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<IGroupRekRepo> _groupRekRepoMock = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock = new();
    private readonly Mock<IAddBillAppService> _addBillAppServiceMock = new();
    private readonly Mock<ILogger<IgdBillingPostingOrchestrator>> _loggerMock = new();

    private readonly IgdBillingPostingOrchestrator _sut;

    public IgdBillingPostingOrchestratorTest()
    {
        _sut = new IgdBillingPostingOrchestrator(
            _nilaiTarifRepoMock.Object,
            _tarifRepoMock.Object,
            _komponenRepoMock.Object,
            _jaminanRepoMock.Object,
            _ppaRepoMock.Object,
            _tindakanRepoMock.Object,
            _tindakanIgdRepoMock.Object,
            _bhpIgdRepoMock.Object,
            _brgRepoMock.Object,
            _groupRekRepoMock.Object,
            _tataRekeningRepoMock.Object,
            _trsBillingRepoMock.Object,
            _addBillAppServiceMock.Object,
            _loggerMock.Object);
    }

    private static IgdVisitModel CreateVisit(string visitId = "IGV001", string regId = "REG001")
    {
        return new IgdVisitModel(
            visitId,
            DateTime.Now,
            new VisitorType("Pasien Test", "L", new DateOnly(1990, 1, 1), "08123"),
            new PpaReff("DOK01", "dr. Test"),
            false,
            IgdVisitTriageType.Default,
            AdministrativeStateEnum.Registered,
            new RegReff(regId, "RM001", "Pasien Test"),
            RedirectionType.Default,
            "BED-01",
            TriageMethodEnum.Unknown,
            TriageColorEnum.Unknown,
            DateTime.MinValue,
            DateTime.MinValue,
            AuditTrailType.Create("U01", DateTime.Now),
            AuditInfoType.Default,
            [],
            []);
    }

    private static RegModel CreateReg(string regId = "REG001")
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 11, 10),
            new AuditInfoType("U01", DateTime.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff("RM001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new TipeJaminanReff("00101", "BPJS Non PBI"),
            PolisModel.Default.ToReff(),
            new KelasReff("K3", "Kelas 3"),
            CaraMasukDkType.Default,
            new RujukanReff("-", "-"),
            new PpaReff("DOK01", "dr. Test"),
            new LayananReff("IGD", "Instalasi Gawat Darurat"),
            new KarcisReff("KRC01", "Karcis IGD"),
            RegEligibilityType.Default,
            []);
    }

    private static TindakanIgdModel CreateTindakanIgd(
        string tindakanIgdId = "TKI001",
        string visitId = "IGV001",
        string tarifId = "TRF01",
        bool isPosted = false)
    {
        return new TindakanIgdModel(
            tindakanIgdId,
            visitId,
            "REG001",
            tarifId,
            "Jahit Luka",
            1,
            ActivityTindakanIgd.Tindakan,
            new PpaReff("DOK01", "dr. Test"),
            new AuditInfoType("U01", DateTime.Now),
            isPosted: isPosted);
    }

    private static BhpIgdModel CreateBhpIgd(
        string bhpIgdId = "BHI001",
        string visitId = "IGV001",
        string brgId = "BRG01",
        bool isPosted = false)
    {
        return new BhpIgdModel(
            bhpIgdId,
            visitId,
            "REG001",
            brgId,
            "Spuit 3cc",
            2,
            5000m,
            new AuditInfoType("U01", DateTime.Now),
            isPosted: isPosted);
    }

    private static JaminanType CreateJaminan(string jaminanId = "001")
    {
        return new JaminanType(
            jaminanId,
            "BPJS",
            true,
            AlamatType.Default,
            CaraBayarDkType.Default,
            new GroupJaminanReff("BPJS", "BPJS"),
            new JaminanTipeTarifType(new TipeTarifReff("BPJS", "Tarif BPJS"), new TipeTarifReff("BPJS", "Tarif BPJS")),
            new JaminanRekeningType(new CoaType("111", "Kas"), new CoaType("112", "Kas"), new CoaType("113", "Kas"), new CoaType("114", "Kas")),
            JaminanTipeBarangType.Default);
    }

    private static NilaiTarifType CreateNilaiTarif(string tarifId = "TRF01", string tipeTarifId = "BPJS", string kelasId = "K3")
    {
        return new NilaiTarifType(
            "NT01",
            tarifId,
            "Jahit Luka",
            new TipeTarifReff(tipeTarifId, "Tarif BPJS"),
            new KelasReff(kelasId, "Kelas 3"),
            50000m,
            [new NilaiTarifKomponenType(1, new KomponenReff("KOMP01", "Jasa Medis"), 50000m)]);
    }

    [Fact]
    public async Task PostTindakanAsync_WhenTindakanAlreadyPosted_ReturnsTrueWithoutRePosting()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var tindakan = CreateTindakanIgd(isPosted: true);

        // Act
        var result = await _sut.PostTindakanAsync(visit, reg, tindakan);

        // Assert
        result.Should().BeTrue();
        _addBillAppServiceMock.Verify(x => x.FromTindakan(
            It.IsAny<TindakanModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<TarifType>(),
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task PostTindakanAsync_WhenTarifNotFound_LogsErrorAndReturnsFalse()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var tindakan = CreateTindakanIgd(tarifId: "TRF_UNKNOWN");

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(CreateJaminan()));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe<TarifType>.None);

        // Act
        var result = await _sut.PostTindakanAsync(visit, reg, tindakan);

        // Assert
        result.Should().BeFalse();
        tindakan.IsPosted.Should().BeFalse();
    }

    [Fact]
    public async Task PostTindakanAsync_WhenNilaiTarifNotFound_LogsErrorAndReturnsFalse()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var tindakan = CreateTindakanIgd(tarifId: "TRF01");

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(CreateJaminan()));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01", TarifName = "Jahit Luka" }));
        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe<NilaiTarifType>.None);

        // Act
        var result = await _sut.PostTindakanAsync(visit, reg, tindakan);

        // Assert
        result.Should().BeFalse();
        tindakan.IsPosted.Should().BeFalse();
    }

    [Fact]
    public async Task PostTindakanAsync_WhenValid_ResolvesTarif3D_CallsAddBillAppService_MarksAsPosted_AndReturnsTrue()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var tindakan = CreateTindakanIgd();
        var jaminan = CreateJaminan();
        var tarif = TarifType.Default with { TarifId = "TRF01", TarifName = "Jahit Luka" };
        var nilaiTarif = CreateNilaiTarif();
        var komp = KomponenType.Default with { KomponenId = "KOMP01", KomponenName = "Jasa Medis" };

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(jaminan));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(tarif));
        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe.From(nilaiTarif));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([komp]);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(PpaType.Default with { PpaId = "DOK01", PpaName = "dr. Test" }));

        var dummyBill = TrsBillType.Default with { TrsBillingId = "BILL_TDK_01" };
        _addBillAppServiceMock.Setup(x => x.FromTindakan(
            It.IsAny<TindakanModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<TarifType>(),
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            It.IsAny<DateTime>()))
            .Returns(dummyBill);

        // Act
        var result = await _sut.PostTindakanAsync(visit, reg, tindakan);

        // Assert
        result.Should().BeTrue();
        tindakan.IsPosted.Should().BeTrue();
        tindakan.TrsBillId.Should().Be("BILL_TDK_01");
        tindakan.TrsTdkId.Should().NotBeNullOrWhiteSpace();
        tindakan.PostedDate.Should().NotBeNull();

        _tindakanRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Once);
        _addBillAppServiceMock.Verify(x => x.FromTindakan(
            It.IsAny<TindakanModel>(), reg, tarif, jaminan, It.IsAny<IEnumerable<KomponenType>>(), It.IsAny<DateTime>()), Times.Once);
        _tindakanIgdRepoMock.Verify(x => x.SaveChanges(tindakan), Times.Once);
    }

    [Fact]
    public async Task PostBhpAsync_WhenBhpAlreadyPosted_ReturnsTrueWithoutRePosting()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var bhp = CreateBhpIgd(isPosted: true);

        // Act
        var result = await _sut.PostBhpAsync(visit, reg, bhp);

        // Assert
        result.Should().BeTrue();
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.IsAny<TrsBillType>()), Times.Never);
    }

    [Fact]
    public async Task PostBhpAsync_WhenJaminanNotFound_LogsErrorAndReturnsFalse()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var bhp = CreateBhpIgd();

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe<JaminanType>.None);

        // Act
        var result = await _sut.PostBhpAsync(visit, reg, bhp);

        // Assert
        result.Should().BeFalse();
        bhp.IsPosted.Should().BeFalse();
    }

    [Fact]
    public async Task PostBhpAsync_WhenValid_PostsBhpToBilling_MarksAsPosted_AndReturnsTrue()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var bhp = CreateBhpIgd();
        var jaminan = CreateJaminan();

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(jaminan));
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe<IBrg>.None);
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        // Act
        var result = await _sut.PostBhpAsync(visit, reg, bhp);

        // Assert
        result.Should().BeTrue();
        bhp.IsPosted.Should().BeTrue();
        bhp.TrsBillId.Should().Be(bhp.BhpIgdId);
        bhp.PostedDate.Should().NotBeNull();

        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.Is<TataRekeningModel>(t => t.RegId == reg.RegId)), Times.Once);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.Is<TrsBillType>(b =>
            b.TrsBillingId == bhp.BhpIgdId &&
            b.ModulGroup == BillModulGroup.Obat &&
            b.Nilai.Total == bhp.Subtotal)), Times.Once);
        _bhpIgdRepoMock.Verify(x => x.SaveChanges(bhp), Times.Once);
    }

    [Fact]
    public async Task SyncAllPendingForVisitAsync_WhenAllItemsSucceed_ReturnsTrue()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var tindakan = CreateTindakanIgd("TKI01");
        var bhp = CreateBhpIgd("BHI01");

        _tindakanIgdRepoMock.Setup(x => x.ListData(visit))
            .Returns([new TindakanIgdView("TKI01", visit.IgdVisitId, reg.RegId, "TRF01", "Luka", 1, ActivityTindakanIgd.Tindakan, new PpaReff("D01", "Dr"), "U01", DateTime.Now, IsPosted: false)]);
        _tindakanIgdRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanIgdKey>(k => k.TindakanIgdId == "TKI01")))
            .Returns(MayBe.From(tindakan));

        _bhpIgdRepoMock.Setup(x => x.ListData(visit))
            .Returns([new BhpIgdView("BHI01", visit.IgdVisitId, reg.RegId, "BRG01", "Kassa", 2, 5000, 10000, DateTime.Now, IsPosted: false)]);
        _bhpIgdRepoMock.Setup(x => x.LoadEntity(It.Is<IBhpIgdKey>(k => k.BhpIgdId == "BHI01")))
            .Returns(MayBe.From(bhp));

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(CreateJaminan()));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01", TarifName = "Luka" }));
        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe.From(CreateNilaiTarif()));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([]);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(PpaType.Default with { PpaId = "D01", PpaName = "Dr" }));
        _addBillAppServiceMock.Setup(x => x.FromTindakan(It.IsAny<TindakanModel>(), It.IsAny<RegModel>(), It.IsAny<TarifType>(), It.IsAny<JaminanType>(), It.IsAny<IEnumerable<KomponenType>>(), It.IsAny<DateTime>()))
            .Returns(TrsBillType.Default with { TrsBillingId = "BILL01" });

        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe<IBrg>.None);
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        // Act
        var result = await _sut.SyncAllPendingForVisitAsync(visit, reg);

        // Assert
        result.Should().BeTrue();
        tindakan.IsPosted.Should().BeTrue();
        bhp.IsPosted.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAllPendingForVisitAsync_WhenTindakanFailsTarif_ReturnsFalse()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var tindakan = CreateTindakanIgd("TKI01");

        _tindakanIgdRepoMock.Setup(x => x.ListData(visit))
            .Returns([new TindakanIgdView("TKI01", visit.IgdVisitId, reg.RegId, "TRF01", "Luka", 1, ActivityTindakanIgd.Tindakan, new PpaReff("D01", "Dr"), "U01", DateTime.Now, IsPosted: false)]);
        _tindakanIgdRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanIgdKey>(k => k.TindakanIgdId == "TKI01")))
            .Returns(MayBe.From(tindakan));
        _bhpIgdRepoMock.Setup(x => x.ListData(visit))
            .Returns([]);

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(CreateJaminan()));
        // Missing tarif -> causes failure
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe<TarifType>.None);

        // Act
        var result = await _sut.SyncAllPendingForVisitAsync(visit, reg);

        // Assert
        result.Should().BeFalse();
        tindakan.IsPosted.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAllPendingForVisitAsync_WhenNoPendingItems_ReturnsTrue()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();

        _tindakanIgdRepoMock.Setup(x => x.ListData(visit))
            .Returns([]);
        _bhpIgdRepoMock.Setup(x => x.ListData(visit))
            .Returns([]);

        // Act
        var result = await _sut.SyncAllPendingForVisitAsync(visit, reg);

        // Assert
        result.Should().BeTrue();
    }
}
