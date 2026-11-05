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

public class TdkSaveTindakanHandlerTest
{
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<IJaminanRepo> _jaminanRepoMock = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<ITindakanRepo> _tindakanRepoMock = new();
    private readonly Mock<IAddBillAppService> _addBillAppServiceMock = new();
    private readonly Mock<IMapJaminanJkRepo> _mapJaminanJkRepoMock = new();
    private readonly Mock<IJurnalRepo> _jurnalRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private readonly TdkSaveTindakanHandler _sut;

    public TdkSaveTindakanHandlerTest()
    {
        _sut = new TdkSaveTindakanHandler(
            _regRepoMock.Object,
            _layananRepoMock.Object,
            _tarifRepoMock.Object,
            _jaminanRepoMock.Object,
            _nilaiTarifRepoMock.Object,
            _komponenRepoMock.Object,
            _ppaRepoMock.Object,
            _tindakanRepoMock.Object,
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
    public async Task GivenNewTindakanWithCustomQty_WhenSave_ThenTindakanCreatedWithMatchingQtyAndProportionalTotal()
    {
        // Arrange: TindakanId is "-" (new entity)
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var cmd = new TdkSaveTindakanCmd(
            "-",
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-01",
            [new TdkSaveTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: 4);

        _tindakanRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == "-")))
            .Returns(MayBe<TindakanModel>.None);

        TindakanModel? savedTindakan = null;

        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(t => savedTindakan = t);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedTindakan.Should().NotBeNull();
        savedTindakan!.Qty.Should().Be(4);
        savedTindakan.Total.Should().Be(200000m);

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

    [Fact]
    public async Task GivenNewTindakanWithIsDiskonDokterTrue_WhenSave_ThenTindakanCreatedWithDiskonDokterAndDoctorWaiverApplied()
    {
        // Arrange: TindakanId is "-" (new entity)
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var cmd = new TdkSaveTindakanCmd(
            "-",
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-01",
            [new TdkSaveTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: 2,
            IsDiskonDokter: true);

        _tindakanRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == "-")))
            .Returns(MayBe<TindakanModel>.None);

        TindakanModel? savedTindakan = null;
        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(t => savedTindakan = t);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedTindakan.Should().NotBeNull();
        savedTindakan!.IsDiskonDokter.Should().BeTrue();
        savedTindakan.DiskonDokter.Should().Be(100000m);
        savedTindakan.Total.Should().Be(0m);
    }

    [Fact]
    public async Task GivenExistingTindakanWithCustomQty_WhenSave_ThenTindakanUpdatedWithMatchingQtyAndPreservesId()
    {
        // Arrange: Existing TindakanId
        const string existingId = "TDK-EXISTING-01";
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var existingTindakan = new TindakanModel(
            existingId,
            new DateTime(2026, 10, 16, 9, 0, 0),
            "",
            new RegReff("REG-01", "-", "-"),
            new LayananReff("LYN-01", "Poli Umum"),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            new TarifReff("TRF-01", "Pemeriksaan Dokter"),
            [],
            AuditTrailType.Create("USER-01", new DateTime(2026, 10, 16, 9, 0, 0)),
            qty: 1);

        _tindakanRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == existingId)))
            .Returns(MayBe.From(existingTindakan));

        var cmd = new TdkSaveTindakanCmd(
            existingId,
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-02",
            [new TdkSaveTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: 2);

        TindakanModel? savedTindakan = null;
        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(t => savedTindakan = t);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TindakanId.Should().Be(existingId);
        savedTindakan.Should().NotBeNull();
        savedTindakan!.TindakanId.Should().Be(existingId);
        savedTindakan.Qty.Should().Be(2);
        savedTindakan.Total.Should().Be(100000m);
        savedTindakan.AuditTrail.Modified.UserId.Should().Be("USER-02");
    }

    [Fact]
    public async Task GivenExistingTindakanWithIsDiskonDokterTrue_WhenSave_ThenTindakanUpdatedWithDiskonDokterApplied()
    {
        // Arrange: Existing TindakanId
        const string existingId = "TDK-EXISTING-02";
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var existingTindakan = new TindakanModel(
            existingId,
            new DateTime(2026, 10, 16, 9, 0, 0),
            "",
            new RegReff("REG-01", "-", "-"),
            new LayananReff("LYN-01", "Poli Umum"),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            new TarifReff("TRF-01", "Pemeriksaan Dokter"),
            [],
            AuditTrailType.Create("USER-01", new DateTime(2026, 10, 16, 9, 0, 0)),
            qty: 1,
            isDiskonDokter: false);

        _tindakanRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == existingId)))
            .Returns(MayBe.From(existingTindakan));

        var cmd = new TdkSaveTindakanCmd(
            existingId,
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-02",
            [new TdkSaveTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: 2,
            IsDiskonDokter: true);

        TindakanModel? savedTindakan = null;
        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(t => savedTindakan = t);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TindakanId.Should().Be(existingId);
        savedTindakan.Should().NotBeNull();
        savedTindakan!.TindakanId.Should().Be(existingId);
        savedTindakan.Qty.Should().Be(2);
        savedTindakan.IsDiskonDokter.Should().BeTrue();
        savedTindakan.DiskonDokter.Should().Be(100000m);
        savedTindakan.Total.Should().Be(0m);
        savedTindakan.AuditTrail.Modified.UserId.Should().Be("USER-02");
    }

    [Fact]
    public async Task GivenSaveWithZeroOrNegativeQty_WhenSave_ThenDefaultsToQty1()
    {
        // Arrange
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var cmd = new TdkSaveTindakanCmd(
            "-",
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-01",
            [new TdkSaveTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: -3);

        _tindakanRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == "-")))
            .Returns(MayBe<TindakanModel>.None);

        TindakanModel? savedTindakan = null;
        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(t => savedTindakan = t);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        savedTindakan.Should().NotBeNull();
        savedTindakan!.Qty.Should().Be(1);
        savedTindakan.Total.Should().Be(50000m);
    }

    [Fact]
    public async Task GivenAddBillAppServiceThrowsInvalidOperationException_WhenSave_ThenRollbacksAndDoesNotSaveChanges()
    {
        // Arrange
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var cmd = new TdkSaveTindakanCmd(
            "-",
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-01",
            [new TdkSaveTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: 1);

        _tindakanRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == "-")))
            .Returns(MayBe<TindakanModel>.None);

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

    [Fact]
    public async Task GivenSaveTindakanWithIsDiskonDokterTrue_WhenQueriedViaTindakanGetHandler_ThenPipelineMaintainsDoctorWaiverIntegrity()
    {
        // Arrange
        SetupBasicMasterData(nilaiTarifKomp: 50000m);
        var cmd = new TdkSaveTindakanCmd(
            "-",
            "REG-01",
            "LYN-01",
            "NT-01",
            "USER-01",
            [new TdkSaveTindakanPpaCmd("KOMP-01", "PPA-01")],
            Qty: 3,
            IsDiskonDokter: true);

        _tindakanRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == "-")))
            .Returns(MayBe<TindakanModel>.None);

        TindakanModel? savedTindakan = null;
        _tindakanRepoMock.Setup(x => x.SaveChanges(It.IsAny<TindakanModel>()))
            .Callback<TindakanModel>(t => savedTindakan = t);

        // Act 1: Save Tindakan with Diskon Dokter
        var saveResult = await _sut.Handle(cmd, CancellationToken.None);

        // Assert 1: Verify Save output
        saveResult.Should().NotBeNull();
        savedTindakan.Should().NotBeNull();
        savedTindakan!.IsDiskonDokter.Should().BeTrue();
        savedTindakan.Qty.Should().Be(3);
        savedTindakan.DiskonDokter.Should().Be(150000m);
        savedTindakan.Total.Should().Be(0m);

        // Act 2: Simulate Get via TindakanGetHandler pipeline
        var getHandler = new TindakanGetHandler(_tindakanRepoMock.Object);
        var query = new TindakanGetQuery(saveResult.TindakanId);
        _tindakanRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == saveResult.TindakanId)))
            .Returns(MayBe.From(savedTindakan));

        var getResult = await getHandler.Handle(query, CancellationToken.None);

        // Assert 2: Verify Get output integrity
        getResult.Should().NotBeNull();
        getResult.TindakanId.Should().Be(saveResult.TindakanId);
        getResult.Qty.Should().Be(3);
        getResult.IsDiskonDokter.Should().BeTrue();
        getResult.DiskonDokter.Should().Be(150000m);
        getResult.Total.Should().Be(0m);
        getResult.ListKomponen.Should().HaveCount(1);

        var kompResult = getResult.ListKomponen.First();
        kompResult.KomponenId.Should().Be("KOMP-01");
        kompResult.Nilai.Should().Be(50000m);
        kompResult.Qty.Should().Be(3);
        kompResult.Diskon.Should().Be(150000m);
        kompResult.SubTotal.Should().Be(0m);
    }
}
