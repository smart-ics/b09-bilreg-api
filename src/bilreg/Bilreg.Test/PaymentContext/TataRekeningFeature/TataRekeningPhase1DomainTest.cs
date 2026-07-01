using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningPhase1DomainTest
{
    private const string REG_ID = "REG-PHASE1-001";
    private static readonly DateTime TestDate = new(2026, 6, 16, 10, 0, 0);

    [Fact]
    public void P1_01_GivenClosedWithoutVerification_WhenAllocate_ThenShouldReject()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-01", 100_000m));
        tataRekening.Close();

        Action act = () => tataRekening.AllocateFinancialResponsibility(
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Financial Verification*");
    }

    [Fact]
    public void P1_02_GivenClosedWithoutVerification_WhenFinalize_ThenShouldReject()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-02", 100_000m));
        tataRekening.Close();

        Action act = () => tataRekening.FinalizeFinancialResponsibility("verif-01", TestDate);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Financial Verification*");
    }

    [Fact]
    public void P1_03_GivenVerifiedWithoutAllocation_WhenFinalize_ThenShouldReject()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-03", 100_000m));
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);

        Action act = () => tataRekening.FinalizeFinancialResponsibility("verif-01", TestDate);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Allocation*");
    }

    [Fact]
    public void P1_04_GivenVerifiedAllocation_WhenAllocateOnly_ThenShouldStayClosedWithProjection()
    {
        var bill = CreateBill("BILL-04", 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening,
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Closed);
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();
        bill.ListFinalization.Should().NotBeEmpty();
        bill.ListFinalization.Sum(x => x.Nilai).Should().Be(100_000m);
    }

    [Fact]
    public void P1_05_GivenPriorAllocation_WhenReAllocate_ThenShouldReplaceProjection()
    {
        var bill = CreateBill("BILL-05", 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening,
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);

        tataRekening.AllocateFinancialResponsibility(
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);

        bill.ListFinalization.Sum(x => x.Nilai).Should().Be(100_000m);
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Closed);
    }

    [Fact]
    public void P1_06_GivenFinalizedTataRekening_WhenInitiateSettlement_ThenShouldSetFlagWithoutStatusChange()
    {
        var tataRekening = CreateFinalized();

        tataRekening.InitiateSettlement("verif-01", TestDate);

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);
        tataRekening.SettlementInitiated.Should().BeTrue();
    }

    [Fact]
    public void P1_07_GivenClosedTataRekening_WhenInitiateSettlement_ThenShouldReject()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-07", 50_000m));
        tataRekening.Close();

        Action act = () => tataRekening.InitiateSettlement("verif-01", TestDate);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FINALIZED*");
    }

    [Fact]
    public void P1_08_GivenPaymentStarted_WhenInitiateSettlement_ThenShouldReject()
    {
        var tataRekening = CreateFinalized();
        tataRekening.Pay(
            [BuildPayment(PaymentType.ByKas, 50_000m, 0m)],
            "PAY-001",
            TestDate);

        Action act = () => tataRekening.InitiateSettlement("verif-01", TestDate);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Payment Settlement*");
    }

    [Fact]
    public void P1_09_GivenClosedWithAllocation_WhenReOpen_ThenShouldClearVerificationAndAllocation()
    {
        var bill = CreateBill("BILL-09", 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening,
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);

        tataRekening.ReOpen();

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Opened);
        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeFalse();
        bill.ListFinalization.Should().BeEmpty();
    }

    [Fact]
    public void P1_10_GivenRequiresAdjustment_WhenAllocate_ThenShouldReject()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-10", 100_000m));
        tataRekening.Close();
        tataRekening.RequireFinancialAdjustment();

        Action act = () => tataRekening.AllocateFinancialResponsibility(
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Financial Adjustment*");
    }

    [Fact]
    public void P1_11_GivenOpenedTataRekening_WhenCloseTwice_ThenSecondShouldReject()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-11", 10_000m));
        tataRekening.Close();

        Action act = () => tataRekening.Close();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OPENED*");
    }

    [Fact]
    public void P1_12_GivenFinalizedTataRekening_WhenInitiateSettlementTwice_ThenSecondShouldReject()
    {
        var tataRekening = CreateFinalized();
        tataRekening.InitiateSettlement("verif-01", TestDate);

        Action act = () => tataRekening.InitiateSettlement("verif-01", TestDate);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah dilakukan*");
    }

    private static TataRekeningModel CreateFinalized()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-FIN", 100_000m));
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);
        return tataRekening;
    }

    private static TataRekeningModel HydrateOpened(params TrsBillType[] listTrsBill) =>
        new(REG_ID, TataRekeningStatusEnum.Opened, TataRekeningFinalizationType.Default, [], listTrsBill);

    private static TataRekeningPaymentType BuildPayment(PaymentType payment, decimal nilaiJasa, decimal nilaiObat) =>
        new(payment, nilaiJasa, nilaiObat, CoaType.Default);

    private static TrsBill2CoaType ValidPdpCoa => new(
        new CoaType("PPDP-01", ""),
        new CoaType("PDPT-01", ""),
        CoaType.Default,
        CoaType.Default,
        CoaType.Default,
        CoaType.Default);

    private static TrsBillType CreateBill(string billId, decimal amount)
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
            new RegReff(REG_ID, "-", "-"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(amount, 0, 0, 0),
            new TrsBillKetType("Test", "", "REF", 1, ""),
            [trans],
            [],
            []);
    }
}
