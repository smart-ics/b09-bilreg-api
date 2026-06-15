using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillCreationDomainServiceTest
{
    private readonly CreateBillService _sut;

    public TrsBillCreationDomainServiceTest()
    {
        _sut = new CreateBillService();
    }

    [Fact]
    public void UT01_GivenClosedTataRekening_WhenCreateFromRegistration_ThenShouldThrowInvalidOperationException()
    {
        var reg = CreateRegWithKomponen("REG-001");
        var tataRekening = new TataRekeningModel(reg.RegId, TataRekeningStatusEnum.Closed, []);

        Action act = () => _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus Closed*");
    }

    [Fact]
    public void UT02_GivenOpenedTataRekening_WhenCreateFromRegistration_ThenShouldCreateTrsBill()
    {
        var reg = CreateRegWithKomponen("REG-002");
        var tataRekening = new TataRekeningModel(reg.RegId, TataRekeningStatusEnum.Opened, []);

        var result = _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be(reg.RegId);
        result.Reg.RegId.Should().Be(reg.RegId);
        result.ListTransaction.Should().HaveCount(1);
    }

    [Fact]
    public void UT03_GivenDifferentRegistration_WhenCreateFromRegistration_ThenShouldThrowArgumentException()
    {
        var reg = CreateRegWithKomponen("REG-003");
        var tataRekening = new TataRekeningModel("REG-LAIN", TataRekeningStatusEnum.Opened, []);

        Action act = () => _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tataRekening");
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
}
