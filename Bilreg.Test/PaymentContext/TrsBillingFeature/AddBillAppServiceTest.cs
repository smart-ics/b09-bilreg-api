using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class AddBillAppServiceTest
{
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock = new();
    private readonly AddBillAppService _sut;

    public AddBillAppServiceTest()
    {
        _sut = new AddBillAppService(
            _tataRekeningRepoMock.Object,
            _trsBillingRepoMock.Object,
            new CreateBillDomService());
    }

    [Fact]
    public void UT01_GivenExistingTataRekening_WhenCreateFromReg_ThenPersistsBillOnly()
    {
        var reg = CreateRegWithKomponen("REG-001");
        var existingTataRekening = TataRekeningModel.Create(reg.RegId);
        _tataRekeningRepoMock
            .Setup(x => x.LoadEntity(reg))
            .Returns(MayBe.From(existingTataRekening));

        var result = _sut.FromReg(
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be(reg.RegId);
        result.ListTransaction.Should().HaveCount(1);

        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.Is<TrsBillType>(b => b.TrsBillingId == reg.RegId)), Times.Once);
    }

    [Fact]
    public void UT02_GivenMissingTataRekening_WhenCreateFromReg_ThenCreatesTataRekeningAndBill()
    {
        var reg = CreateRegWithKomponen("REG-002");
        _tataRekeningRepoMock
            .Setup(x => x.LoadEntity(reg))
            .Returns(MayBe<TataRekeningModel>.None);

        var result = _sut.FromReg(
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be(reg.RegId);
        result.ListTransaction.Should().HaveCount(1);

        _tataRekeningRepoMock.Verify(
            x => x.SaveChanges(It.Is<TataRekeningModel>(t =>
                t.RegId == reg.RegId && t.Status == TataRekeningStatusEnum.Opened)),
            Times.Once);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.Is<TrsBillType>(b => b.TrsBillingId == reg.RegId)), Times.Once);
    }

    [Fact]
    public void UT03_GivenExistingTataRekening_WhenCreateFromTindakan_ThenPersistsBillOnly()
    {
        var reg = CreateRegForTindakan("REG-003");
        var tindakan = CreateTindakan("TDK-001", reg);
        var existingTataRekening = TataRekeningModel.Create(reg.RegId);
        _tataRekeningRepoMock
            .Setup(x => x.LoadEntity(reg))
            .Returns(MayBe.From(existingTataRekening));

        var result = _sut.FromTindakan(
            tindakan,
            reg,
            TarifType.Default,
            CreateJaminanForReg(reg),
            [KomponenType.Default with { KomponenId = "KOMP-01", KomponenName = "Komponen Test" }]);

        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be(tindakan.TindakanId);
        result.ListTransaction.Should().HaveCount(1);

        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _trsBillingRepoMock.Verify(
            x => x.SaveChanges(It.Is<TrsBillType>(b => b.TrsBillingId == tindakan.TindakanId)),
            Times.Once);
    }

    [Fact]
    public void UT04_GivenMissingTataRekening_WhenCreateFromTindakan_ThenCreatesTataRekeningAndBill()
    {
        var reg = CreateRegForTindakan("REG-004");
        var tindakan = CreateTindakan("TDK-002", reg);
        _tataRekeningRepoMock
            .Setup(x => x.LoadEntity(reg))
            .Returns(MayBe<TataRekeningModel>.None);

        var result = _sut.FromTindakan(
            tindakan,
            reg,
            TarifType.Default,
            CreateJaminanForReg(reg),
            [KomponenType.Default with { KomponenId = "KOMP-01", KomponenName = "Komponen Test" }]);

        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be(tindakan.TindakanId);
        result.ListTransaction.Should().HaveCount(1);

        _tataRekeningRepoMock.Verify(
            x => x.SaveChanges(It.Is<TataRekeningModel>(t =>
                t.RegId == reg.RegId && t.Status == TataRekeningStatusEnum.Opened)),
            Times.Once);
        _trsBillingRepoMock.Verify(
            x => x.SaveChanges(It.Is<TrsBillType>(b => b.TrsBillingId == tindakan.TindakanId)),
            Times.Once);
    }

    private static RegModel CreateRegWithKomponen(string regId)
    {
        var komponen = new RegKomponenType(
            new KomponenReff("KOMP-01", "Komponen Test"),
            PpaType.Default.ToReff(),
            15000m,
            1000m);

        return new RegModel(
            regId,
            new DateOnly(2026, 6, 14),
            new AuditInfoType("tester", new DateTime(2026, 6, 14, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            "-",
            "-",
            [komponen]);
    }

    private static RegModel CreateRegForTindakan(string regId)
    {
        var tipeJaminan = new TipeJaminanReff("J0011", "Jaminan Test");

        return new RegModel(
            regId,
            new DateOnly(2026, 6, 14),
            new AuditInfoType("tester", new DateTime(2026, 6, 14, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            tipeJaminan,
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            "-",
            "-",
            []);
    }

    private static JaminanType CreateJaminanForReg(RegModel reg) =>
        JaminanType.Default with { JaminanId = reg.TipeJaminan.TipeJaminanId[..3] };

    private static TindakanModel CreateTindakan(string tindakanId, RegModel reg)
    {
        var komponen = new KomponenReff("KOMP-01", "Komponen Test");
        var listKomponen = new List<TindakanKomponenWithoutPpaType>
        {
            new(komponen, 0, 15000m, 1, 15000m)
        };

        return new TindakanModel(
            tindakanId,
            new DateTime(2026, 6, 14, 9, 0, 0),
            "",
            reg.ToReff(),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            TarifType.Default.ToReff(),
            listKomponen,
            AuditTrailType.Default);
    }
}
