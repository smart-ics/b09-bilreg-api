using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
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
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
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
using Bilreg.Domain.SalesContext.PenjualanFeature;
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
    private readonly Mock<ITindakanIgdRepo> _tindakanIgdRepoMock = new();
    private readonly Mock<IBhpIgdRepo> _bhpIgdRepoMock = new();
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<IGroupRekRepo> _groupRekRepoMock = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock = new();
    private readonly Mock<IBrgPricingService> _brgPricingServiceMock = new();
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
            _tindakanIgdRepoMock.Object,
            _bhpIgdRepoMock.Object,
            _brgRepoMock.Object,
            _groupRekRepoMock.Object,
            _tataRekeningRepoMock.Object,
            _trsBillingRepoMock.Object,
            _loggerMock.Object,
            _brgPricingServiceMock.Object);
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

    private static RegModel CreateReg(string regId = "REG001", KelasReff? kelas = null)
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
            kelas ?? new KelasReff("K3", "Kelas 3"),
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
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.IsAny<TrsBillType>()), Times.Never);
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
    public async Task PostTindakanAsync_WhenValid_ResolvesTarif3D_SavesDirectToBilling_MarksAsPosted_AndReturnsTrue()
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

        // Act
        var result = await _sut.PostTindakanAsync(visit, reg, tindakan);

        // Assert
        result.Should().BeTrue();
        tindakan.IsPosted.Should().BeTrue();
        tindakan.TrsBillId.Should().Be(tindakan.TindakanIgdId);
        tindakan.TrsTdkId.Should().Be(tindakan.TindakanIgdId);
        tindakan.PostedDate.Should().NotBeNull();

        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.Is<TrsBillType>(b => b.TrsBillingId == tindakan.TindakanIgdId)), Times.Once);
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

    [Fact]
    public async Task PostTindakanAsync_WithMultiPpaPerKomponen_MapsMatchingKomponenAndFallbacksMissingToPrimaryPpa()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var tindakan = new TindakanIgdModel(
            "TKI002",
            visit.IgdVisitId,
            reg.RegId,
            "TRF01",
            "Jahit Luka",
            2,
            ActivityTindakanIgd.Tindakan,
            new PpaReff("DOK01", "dr. Test"),
            new AuditInfoType("U01", DateTime.Now),
            listPpa: [new TindakanIgdPpaType("KOMP02", "PRW01", "Ns. Ani")]);

        var nilaiTarif = new NilaiTarifType(
            "NT01",
            "TRF01",
            "Jahit Luka",
            new TipeTarifReff("BPJS", "Tarif BPJS"),
            new KelasReff("K3", "Kelas 3"),
            100000m,
            [
                new NilaiTarifKomponenType(1, new KomponenReff("KOMP01", "Jasa Medis"), 50000m),
                new NilaiTarifKomponenType(2, new KomponenReff("KOMP02", "Jasa Perawat"), 30000m),
                new NilaiTarifKomponenType(3, new KomponenReff("KOMP03", "Sarana"), 20000m),
            ]);

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(CreateJaminan()));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01", TarifName = "Jahit Luka" }));
        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe.From(nilaiTarif));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([]);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOK01")))
            .Returns(MayBe.From(PpaType.Default with { PpaId = "DOK01", PpaName = "dr. Test" }));

        TrsBillType? savedBill = null;
        _trsBillingRepoMock.Setup(x => x.SaveChanges(It.IsAny<TrsBillType>()))
            .Callback<TrsBillType>(b => savedBill = b);

        // Act
        var result = await _sut.PostTindakanAsync(visit, reg, tindakan);

        // Assert
        result.Should().BeTrue();
        savedBill.Should().NotBeNull();
        savedBill!.Nilai.Total.Should().Be(200000m);
        savedBill.ListTransaction.Should().HaveCount(3);
        savedBill.ListTransaction.Single(e => e.Komponen.BillKompId == "KOMP01").PetugasMedis.PpaId.Should().Be("DOK01");
        savedBill.ListTransaction.Single(e => e.Komponen.BillKompId == "KOMP02").PetugasMedis.PpaId.Should().Be("PRW01");
        savedBill.ListTransaction.Single(e => e.Komponen.BillKompId == "KOMP02").PetugasMedis.PpaName.Should().Be("Ns. Ani");
        savedBill.ListTransaction.Single(e => e.Komponen.BillKompId == "KOMP03").PetugasMedis.PpaId.Should().Be("DOK01");
    }

    [Fact]
    public async Task PostTindakanAsync_WithBomIsChargeBarangTrue_PostsDeterministicBomBillAndUpdatesTrsBillingId()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg();
        var listBarang = new List<TindakanIgdBarangType>
        {
            new(1, "BRG01", "PCS", 2, "PCS", 2, 2, 0, true, "PKB01", "", false),
            new(2, "BRG02", "PCS", 1, "PCS", 1, 1, 0, false, "PKB01", "", false),
            new(3, "BRG03", "PCS", 3, "PCS", 3, 3, 0, true, "PKB01", "", true),
        };

        var tindakan = new TindakanIgdModel(
            "TKI003",
            visit.IgdVisitId,
            reg.RegId,
            "TRF01",
            "Jahit Luka",
            1,
            ActivityTindakanIgd.Tindakan,
            new PpaReff("DOK01", "dr. Test"),
            new AuditInfoType("U01", DateTime.Now),
            listBarang: listBarang);

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(CreateJaminan()));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01", TarifName = "Jahit Luka", IsChargeBarang = true }));
        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe.From(CreateNilaiTarif()));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([]);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(PpaType.Default with { PpaId = "DOK01", PpaName = "dr. Test" }));

        var brgMock = new Mock<IBrg>();
        brgMock.SetupGet(x => x.BrgId).Returns("BRG01");
        brgMock.SetupGet(x => x.BrgName).Returns("Kassa Steril");
        brgMock.SetupGet(x => x.GroupRek).Returns(new GroupRekReff("GR01", "BHP"));
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01")))
            .Returns(MayBe.From(brgMock.Object));

        _brgPricingServiceMock.Setup(x => x.CalculateItemPrice("BRG01", 2m, reg.TipeJaminan.TipeJaminanId, reg.Layanan.LayananId))
            .Returns(new BrgPriceCalcResult(
                new TipeBrgReff("-", "-"),
                new NilaiItemType(7500m, 0m, 0m, 15000m, 0m, 0m, 0m, 0m, 0m, 15000m)));

        var savedBills = new List<TrsBillType>();
        _trsBillingRepoMock.Setup(x => x.SaveChanges(It.IsAny<TrsBillType>()))
            .Callback<TrsBillType>(b => savedBills.Add(b));

        // Act
        var result = await _sut.PostTindakanAsync(visit, reg, tindakan);

        // Assert
        result.Should().BeTrue();
        savedBills.Should().HaveCount(2);
        savedBills.Should().ContainSingle(b => b.TrsBillingId == "TKI003" && b.ModulGroup == BillModulGroup.Jasa);
        var bomBill = savedBills.Should().ContainSingle(b => b.TrsBillingId == "TKI003-B01" && b.ModulGroup == BillModulGroup.Obat).Subject;
        bomBill.Nilai.Total.Should().Be(15000m);
        bomBill.Keterangan.Keterangan.Should().Be("Kassa Steril");
        bomBill.Keterangan.Qty.Should().Be(2m);

        tindakan.ListBarang[0].TrsBillingId.Should().Be("TKI003-B01");
        tindakan.ListBarang[1].TrsBillingId.Should().BeEmpty();
        tindakan.ListBarang[2].TrsBillingId.Should().BeEmpty();
        _tindakanIgdRepoMock.Verify(x => x.SaveChanges(tindakan), Times.Once);
    }

    [Fact]
    public async Task PostTindakanAsync_RecalculatesNilaiTarifUsingRegKelasAndJaminanTipeTarif()
    {
        // Arrange
        var visit = CreateVisit();
        var reg = CreateReg(kelas: new KelasReff("K2", "Kelas 2"));
        var tindakan = CreateTindakanIgd();
        var jaminan = CreateJaminan() with
        {
            TipeTarif = new JaminanTipeTarifType(new TipeTarifReff("VIP", "Tarif VIP"), new TipeTarifReff("VIP", "Tarif VIP"))
        };

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(jaminan));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01", TarifName = "Jahit Luka" }));
        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(It.Is<INilaiTarifCompositKey>(k =>
                k.TarifId == "TRF01" && k.TipeTarifId == "VIP" && k.KelasId == "K2")))
            .Returns(MayBe.From(CreateNilaiTarif("TRF01", "VIP", "K2")));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([]);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(PpaType.Default with { PpaId = "DOK01", PpaName = "dr. Test" }));

        // Act
        var result = await _sut.PostTindakanAsync(visit, reg, tindakan);

        // Assert
        result.Should().BeTrue();
        _nilaiTarifRepoMock.Verify(x => x.LoadEntity(It.Is<INilaiTarifCompositKey>(k =>
            k.TarifId == "TRF01" && k.TipeTarifId == "VIP" && k.KelasId == "K2")), Times.Once);
    }
}
