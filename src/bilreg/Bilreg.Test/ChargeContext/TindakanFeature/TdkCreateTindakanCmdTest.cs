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
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TdkCreateTindakanCmdTest
{
    private readonly Mock<ITindakanRepo> _tindakanRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<IJaminanRepo> _jaminanRepoMock = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock = new();
    private readonly Mock<IAddBillAppService> _addBillAppServiceMock = new();
    private readonly Mock<IMapJaminanJkRepo> _mapJaminanJkRepoMock = new();
    private readonly Mock<IJurnalRepo> _jurnalRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private readonly TindakanCreateHandler _sut;
    private readonly DateTime _testNow = new(2026, 10, 20, 10, 0, 0);

    public TdkCreateTindakanCmdTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(_testNow);

        _sut = new TindakanCreateHandler(
            _tindakanRepoMock.Object,
            _regRepoMock.Object,
            _layananRepoMock.Object,
            _nilaiTarifRepoMock.Object,
            _komponenRepoMock.Object,
            _ppaRepoMock.Object,
            _tarifRepoMock.Object,
            _jaminanRepoMock.Object,
            _trsBillingRepoMock.Object,
            _addBillAppServiceMock.Object,
            _mapJaminanJkRepoMock.Object,
            _jurnalRepoMock.Object,
            _tglJamProviderMock.Object);

        SetupCommonMocks();
    }

    private void SetupCommonMocks()
    {
        var reg = CreateActiveReg();
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(LayananType.Default));

        var komp1 = new KomponenReff("K01", "Jasa Dokter");
        var listKomp = new List<NilaiTarifKomponenType> { new(1, komp1, 50000m) };
        var nilaiTarif = new NilaiTarifType("NT01", "TRF01", "Visite Dokter",
            TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(), 50000m, listKomp);
        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<INilaiTarifKey>()))
            .Returns(MayBe.From(nilaiTarif));

        var tarif = TarifType.Default with { TarifId = "TRF01", TarifName = "Visite Dokter" };
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(tarif));

        _jaminanRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Umum));

        var satTugas = SatTugasType.Default with { SatTugasId = "MD", SatTugasName = "Dokter" };
        var komp = new KomponenType("K01", "Jasa Dokter", GroupKomponenType.Default, CoaType.Default, CoaType.Default, [satTugas]);
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>()))
            .Returns([komp]);

        var ppaSatTugas = new PpaSatTugasType(satTugas, isUtama: true);
        var ppa = new PpaType("PPA01", "dr. Budi", "Budi", SmfType.Default, GroupSpesialisType.Default, [], [ppaSatTugas], []);
        _ppaRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(ppa));

        _mapJaminanJkRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(MapJaminanJkType.Default));

        var dummyBill = TataRekeningTestDataBuilder.CreateBill("BILL01", "REG01", 150000m);
        _addBillAppServiceMock.Setup(x => x.FromTindakan(
            It.IsAny<TindakanModel>(), It.IsAny<RegModel>(), It.IsAny<TarifType>(),
            It.IsAny<JaminanType>(), It.IsAny<IEnumerable<KomponenType>>(), It.IsAny<DateTime>()))
            .Returns(dummyBill);
    }

    private static RegModel CreateActiveReg()
    {
        var reg = RegModel.Default;
        reg.ApplyJaminan(TipeJaminanType.BayarSendiri, PolisModel.Default);
        return reg;
    }

    [Fact]
    public async Task Handle_WithMultiQtyAndPakaiBedId_ShouldPassToModelAndPersist()
    {
        // Arrange
        var ppaCmd = new TdkCreateTindakanPpaCmd("K01", "PPA01");
        var cmd = new TdkCreateTindakanCmd(
            "REG01", "LAY01", "NT01", "USER01",
            [ppaCmd], PakaiBedId: "BED-VIP-01", Qty: 3);

        TindakanModel? capturedModel = null;
        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(m => capturedModel = m);

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TindakanId.Should().NotBeNullOrWhiteSpace();

        capturedModel.Should().NotBeNull();
        capturedModel!.PakaiBedId.Should().Be("BED-VIP-01");
        capturedModel.Qty.Should().Be(3);
        capturedModel.Total.Should().Be(150000m); // 50000 * 3
        capturedModel.ListKomponen.Should().HaveCount(1);
        capturedModel.ListKomponen.First().Qty.Should().Be(3);
        capturedModel.ListKomponen.First().SubTotal.Should().Be(150000m);

        _tindakanRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Once);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.IsAny<TrsBillType>()), Times.Once);
        _jurnalRepoMock.Verify(x => x.SaveChanges(It.IsAny<JurnalType>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDefaultParameters_ShouldUseDefaultQty1AndEmptyBed()
    {
        // Arrange
        var ppaCmd = new TdkCreateTindakanPpaCmd("K01", "PPA01");
        var cmd = new TdkCreateTindakanCmd("REG01", "LAY01", "NT01", "USER01", [ppaCmd]);

        TindakanModel? capturedModel = null;
        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(m => capturedModel = m);

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        capturedModel.Should().NotBeNull();
        capturedModel!.PakaiBedId.Should().BeEmpty();
        capturedModel.Qty.Should().Be(1);
        capturedModel.Total.Should().Be(50000m);
    }

    [Fact]
    public async Task Handle_WhenListPpaIsNull_ShouldThrowArgumentException()
    {
        // Arrange
        var cmd = new TdkCreateTindakanCmd("REG01", "LAY01", "NT01", "USER01", null!);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*List PPA tidak boleh null*");
    }

    [Fact]
    public async Task Handle_WhenQtyLessThan1_ShouldThrowArgumentException()
    {
        // Arrange
        var ppaCmd = new TdkCreateTindakanPpaCmd("K01", "PPA01");
        var cmd = new TdkCreateTindakanCmd(
            "REG01", "LAY01", "NT01", "USER01",
            [ppaCmd], PakaiBedId: "BED-01", Qty: 0);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Qty must be at least 1*");
    }
}
