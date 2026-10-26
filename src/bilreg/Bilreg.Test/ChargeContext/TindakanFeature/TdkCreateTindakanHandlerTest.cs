using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TdkCreateTindakanHandlerTest
{
    private readonly Mock<ITindakanRepo> _tindakanRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<IJaminanRepo> _jaminanRepoMock = new();
    private readonly Mock<IAddBillAppService> _addBillAppServiceMock = new();
    private readonly Mock<IMapJaminanJkRepo> _mapJaminanJkRepoMock = new();
    private readonly Mock<IJurnalRepo> _jurnalRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private readonly TindakanCreateHandler _sut;

    public TdkCreateTindakanHandlerTest()
    {
        _sut = new TindakanCreateHandler(
            _tindakanRepoMock.Object,
            _regRepoMock.Object,
            _layananRepoMock.Object,
            _nilaiTarifRepoMock.Object,
            _komponenRepoMock.Object,
            _ppaRepoMock.Object,
            _tarifRepoMock.Object,
            _jaminanRepoMock.Object,
            _addBillAppServiceMock.Object,
            _mapJaminanJkRepoMock.Object,
            _jurnalRepoMock.Object,
            _tglJamProviderMock.Object);

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 16, 10, 0, 0));
        _mapJaminanJkRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe<MapJaminanJkType>.None);
    }

    private void SetupBasicMasterData(
        string regId = "REG-01",
        string lynId = "LYN-01",
        string nilaiTarifId = "NT-01",
        string tarifId = "TRF-01",
        string jaminanId = "J01",
        string kompId = "KOMP-01",
        string ppaId = "PPA-01",
        decimal nilaiTarifKomp = 50000m)
    {
        var reg = new RegModel(
            regId,
            new DateOnly(2026, 10, 16),
            new AuditInfoType("USER-01", new DateTime(2026, 10, 16, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff($"{jaminanId}01", "BPJS Kesehatan"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

        var layanan = LayananType.Default with { LayananId = lynId, LayananName = "Poli Umum" };
        var satTugas = new SatTugasType("ST1", "Dokter", ProfesiType.Dokter);
        var komp = new KomponenType(kompId, "Jasa Medis", GroupKomponenType.Default, CoaType.Default, CoaType.Default, [satTugas]);
        var nilaiTarif = new NilaiTarifType(
            nilaiTarifId,
            tarifId,
            "Pemeriksaan Dokter",
            TipeTarifType.Default.ToReff(),
            KelasType.Default.ToReff(),
            nilaiTarifKomp,
            [new NilaiTarifKomponenType(0, new KomponenReff(kompId, "Jasa Medis"), nilaiTarifKomp)]);
        var tarif = TarifType.Default with { TarifId = tarifId, TarifName = "Pemeriksaan Dokter" };
        var jaminan = JaminanType.Default with { JaminanId = jaminanId, JaminanName = "BPJS Kesehatan" };
        var ppa = new PpaType(ppaId, "dr. Budi", "", SmfType.Default, GroupSpesialisType.Default, [], [new PpaSatTugasType(satTugas, true)], []);

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId))).Returns(MayBe.From(reg));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == lynId))).Returns(MayBe.From(layanan));
        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(It.Is<INilaiTarifKey>(k => k.NilaiTarifId == nilaiTarifId))).Returns(MayBe.From(nilaiTarif));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == tarifId))).Returns(MayBe.From(tarif));
        _jaminanRepoMock.Setup(x => x.LoadEntity(It.Is<IJaminanKey>(k => k.JaminanId == jaminanId))).Returns(MayBe.From(jaminan));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>())).Returns([komp]);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == ppaId))).Returns(MayBe.From(ppa));

        _addBillAppServiceMock.Setup(x => x.FromTindakan(
                It.IsAny<TindakanModel>(),
                It.IsAny<RegModel>(),
                It.IsAny<TarifType>(),
                It.IsAny<JaminanType>(),
                It.IsAny<IEnumerable<KomponenType>>(),
                It.IsAny<DateTime>()))
            .Returns((TindakanModel t, RegModel r, TarifType trf, JaminanType jmn, IEnumerable<KomponenType> k, DateTime d) =>
                TrsBillFactory.CreateFromTindakan(t, r, trf, jmn, k, d));
    }

    [Fact]
    public async Task GivenValidRequestWithCustomQty_WhenCreate_ThenTindakanCreatedWithMatchingQtyAndProportionalTotal()
    {
        // Arrange
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var cmd = new TdkCreateTindakanCmd(
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-01",
            [new TdkCreateTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: 3);

        TindakanModel? savedTindakan = null;

        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(t => savedTindakan = t);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TindakanId.Should().NotBeNullOrWhiteSpace();

        savedTindakan.Should().NotBeNull();
        savedTindakan!.Qty.Should().Be(3);
        savedTindakan.Total.Should().Be(150000m);
        savedTindakan.ListKomponen.Should().HaveCount(1);
        savedTindakan.ListKomponen.First().Qty.Should().Be(3);
        savedTindakan.ListKomponen.First().SubTotal.Should().Be(150000m);

        _addBillAppServiceMock.Verify(x => x.FromTindakan(
            It.IsAny<TindakanModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<TarifType>(),
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            It.IsAny<DateTime>()), Times.Once);
        _tindakanRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Once);
        _jurnalRepoMock.Verify(x => x.SaveChanges(It.IsAny<JurnalType>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public async Task GivenValidRequestWithZeroOrNegativeQty_WhenCreate_ThenDefaultsToQty1(int nonPositiveQty)
    {
        // Arrange
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var cmd = new TdkCreateTindakanCmd(
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-01",
            [new TdkCreateTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: nonPositiveQty);

        TindakanModel? savedTindakan = null;

        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(t => savedTindakan = t);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedTindakan.Should().NotBeNull();
        savedTindakan!.Qty.Should().Be(1);
        savedTindakan.Total.Should().Be(50000m);
    }

    [Fact]
    public async Task GivenDefaultConstructor_WhenCreate_ThenDefaultsToQty1()
    {
        // Arrange (backward compatibility check)
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var cmd = new TdkCreateTindakanCmd(
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-01",
            [new TdkCreateTindakanPpaCmd("KOMP-01", "PPA-01")]);

        TindakanModel? savedTindakan = null;
        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(t => savedTindakan = t);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedTindakan.Should().NotBeNull();
        savedTindakan!.Qty.Should().Be(1);
    }

    [Fact]
    public async Task GivenNullListPpa_WhenCreate_ThenThrowsArgumentException()
    {
        // Arrange
        var cmd = new TdkCreateTindakanCmd("REG-01", "LYN-01", "NT-01", "USER-01", null!, 1);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*List PPA tidak boleh null*");
    }

    [Fact]
    public async Task GivenAddBillAppServiceThrowsInvalidOperationException_WhenCreate_ThenRollbacksAndDoesNotSaveChanges()
    {
        // Arrange
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var cmd = new TdkCreateTindakanCmd(
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-01",
            [new TdkCreateTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: 1);

        _addBillAppServiceMock.Setup(x => x.FromTindakan(
                It.IsAny<TindakanModel>(),
                It.IsAny<RegModel>(),
                It.IsAny<TarifType>(),
                It.IsAny<JaminanType>(),
                It.IsAny<IEnumerable<KomponenType>>(),
                It.IsAny<DateTime>()))
            .Throws(new InvalidOperationException("Operasi bill hanya diizinkan saat TataRekening untuk registrasi 'REG-01' berstatus OPEN."));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OPEN*");

        _addBillAppServiceMock.Verify(x => x.FromTindakan(
            It.IsAny<TindakanModel>(),
            It.IsAny<RegModel>(),
            It.IsAny<TarifType>(),
            It.IsAny<JaminanType>(),
            It.IsAny<IEnumerable<KomponenType>>(),
            It.IsAny<DateTime>()), Times.Once);
        _tindakanRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Never);
        _jurnalRepoMock.Verify(x => x.SaveChanges(It.IsAny<JurnalType>()), Times.Never);
    }
}
