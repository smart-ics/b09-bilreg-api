using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class CreateBillDomServiceTest
{
    private readonly CreateBillDomService _sut;

    public CreateBillDomServiceTest()
    {
        _sut = new CreateBillDomService();
    }

    [Fact]
    public void UT01_GivenClosedTataRekening_WhenCreateFromRegistration_ThenShouldThrowInvalidOperationException()
    {
        var reg = CreateRegWithKomponen("REG-001");
        var tataRekening = TataRekeningModel.Create(reg.RegId);
        tataRekening.Close();

        Action act = () => _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT02_GivenOpenedTataRekening_WhenCreateFromRegistration_ThenShouldCreateTrsBill()
    {
        var reg = CreateRegWithKomponen("REG-002");
        var tataRekening = TataRekeningModel.Create(reg.RegId);

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
        tataRekening.ListTrsBill.Should().BeEmpty();
    }

    [Fact]
    public void UT04_GivenFinalizedTataRekening_WhenCreateFromRegistration_ThenShouldThrowInvalidOperationException()
    {
        var reg = CreateRegWithKomponen("REG-004");
        var tataRekening = HydrateOpened(reg.RegId, CreateMinimalBill(reg.RegId, 10_000m));
        tataRekening.Close();
        tataRekening.FinalizeFinancialResponsibility(
            [new TataRekeningPaymentType(PaymentType.ByKas, 10_000m, 0m, CoaType.Default)],
            "kasir",
            DateTime.Now);

        Action act = () => _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT05_GivenLunasTataRekening_WhenCreateFromRegistration_ThenShouldThrowInvalidOperationException()
    {
        var reg = CreateRegWithKomponen("REG-005");
        var tataRekening = HydrateOpened(reg.RegId, CreateMinimalBill(reg.RegId, 10_000m));
        tataRekening.Close();
        tataRekening.FinalizeFinancialResponsibility(
            [new TataRekeningPaymentType(PaymentType.ByKas, 10_000m, 0m, CoaType.Default)],
            "kasir",
            DateTime.Now);
        tataRekening.Pay(
            [new TataRekeningPaymentType(PaymentType.ByKas, 10_000m, 0m, CoaType.Default)],
            "PAY-001",
            DateTime.Now);

        Action act = () => _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT06_GivenDifferentRegistration_WhenCreateFromRegistration_ThenShouldThrowArgumentException()
    {
        var reg = CreateRegWithKomponen("REG-003");
        var tataRekening = TataRekeningModel.Create("REG-LAIN");

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

    private static TataRekeningModel HydrateOpened(string regId, params TrsBillType[] listTrsBill) =>
        new(regId, TataRekeningStatusEnum.Opened, TataRekeningFinalizationType.Default, [], listTrsBill);

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
            RegEligibilityType.Default,
            [komponen]);
    }

    private static TrsBill2CoaType ValidPdpCoa => new(
        new CoaType("PPDP-01", ""),
        new CoaType("PDPT-01", ""),
        CoaType.Default,
        CoaType.Default,
        CoaType.Default,
        CoaType.Default);

    private static TrsBillType CreateMinimalBill(string billId, decimal amount)
    {
        var komponen = new TrsBill2KomponenType("KOMP-01", "Komponen Test");
        var trans = TrsBill2TransEventType.Create(
            0,
            komponen,
            TrsBillJenisBayarType.Pdp,
            amount,
            PpaType.Default.ToReff(),
            ValidPdpCoa);

        return new TrsBillType(
            billId,
            BillModulGroup.Jasa,
            new DateTime(2026, 6, 16),
            new RegReff(billId, "-", "-"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(amount, 0, 0, 0),
            TrsBillKetType.Default,
            [trans],
            [],
            []);
    }
}
