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

public class TataRekeningPhase2DomainTest
{
    private const string SourceRegId = "REG-SRC-001";
    private const string TargetRegId = "REG-TGT-001";
    private static readonly DateTime TestDate = new(2026, 6, 16, 10, 0, 0);

    #region MergeRequest lifecycle

    [Fact]
    public void P2_01_GivenNewRequest_WhenCreate_ThenShouldBePending()
    {
        var request = MergeRequestModel.Create("MR-001", SourceRegId, TargetRegId);

        request.Status.Should().Be(MergeRequestStatusEnum.Pending);
        request.SourceRegId.Should().Be(SourceRegId);
        request.TargetRegId.Should().Be(TargetRegId);
    }

    [Fact]
    public void P2_02_GivenPendingRequest_WhenExecute_ThenShouldBeExecuted()
    {
        var request = MergeRequestModel.Create("MR-002", SourceRegId, TargetRegId);

        request.Execute();

        request.Status.Should().Be(MergeRequestStatusEnum.Executed);
    }

    [Fact]
    public void P2_03_GivenPendingRequest_WhenCancel_ThenShouldBeCancelled()
    {
        var request = MergeRequestModel.Create("MR-003", SourceRegId, TargetRegId);

        request.Cancel();

        request.Status.Should().Be(MergeRequestStatusEnum.Cancelled);
    }

    [Fact]
    public void P2_04_GivenExecutedRequest_WhenExecuteAgain_ThenShouldReject()
    {
        var request = MergeRequestModel.Create("MR-004", SourceRegId, TargetRegId);
        request.Execute();

        Action act = () => request.Execute();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Pending*");
    }

    [Fact]
    public void P2_05_GivenPendingWithoutTarget_WhenExecute_ThenShouldReject()
    {
        var request = MergeRequestModel.Create("MR-005", SourceRegId);

        Action act = () => request.Execute();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Target*");
    }

    #endregion

    #region Merge Billing

    [Fact]
    public void P2_06_GivenValidMerge_WhenExecute_ThenShouldMoveBillsAndExecuteRequest()
    {
        var sourceBill = CreateBill("BILL-SRC", SourceRegId, 50_000m);
        var targetBill = CreateBill("BILL-TGT", TargetRegId, 30_000m);
        var source = Hydrate(SourceRegId, TataRekeningStatusEnum.Opened, sourceBill);
        var target = Hydrate(TargetRegId, TataRekeningStatusEnum.Opened, targetBill);
        source.Close();
        target.Close();

        var request = MergeRequestModel.Create("MR-006", SourceRegId, TargetRegId);
        var result = TataRekeningDomainTestHelper.MergeBillingService.Execute(request, source, target);

        result.MergeRequest.Status.Should().Be(MergeRequestStatusEnum.Executed);
        source.ListTrsBill.Should().BeEmpty();
        target.ListTrsBill.Should().HaveCount(2);
        target.ListTrsBill.Should().Contain(b => b.TrsBillingId == "BILL-SRC");
        target.ListTrsBill.Single(b => b.TrsBillingId == "BILL-SRC").Reg.RegId.Should().Be(TargetRegId);
        target.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);
        sourceBill.Nilai.Total.Should().Be(50_000m);
    }

    [Fact]
    public void P2_07_GivenTargetNotClosed_WhenMerge_ThenShouldReject()
    {
        var source = HydrateClosed(SourceRegId, CreateBill("BILL-S", SourceRegId, 10_000m));
        var target = Hydrate(TargetRegId, TataRekeningStatusEnum.Opened, CreateBill("BILL-T", TargetRegId, 10_000m));
        var request = MergeRequestModel.Create("MR-007", SourceRegId, TargetRegId);

        Action act = () => TataRekeningDomainTestHelper.MergeBillingService.Execute(request, source, target);

        act.Should().Throw<InvalidOperationException>().WithMessage("*CLOSED*");
    }

    [Fact]
    public void P2_08_GivenSourceLunas_WhenMerge_ThenShouldReject()
    {
        var source = CreateLunas(SourceRegId);
        var target = HydrateClosed(TargetRegId, CreateBill("BILL-T", TargetRegId, 10_000m));
        var request = MergeRequestModel.Create("MR-008", SourceRegId, TargetRegId);

        Action act = () => TataRekeningDomainTestHelper.MergeBillingService.Execute(request, source, target);

        act.Should().Throw<InvalidOperationException>().WithMessage("*LUNAS*");
    }

    [Fact]
    public void P2_09_GivenSettledSourceBill_WhenMerge_ThenShouldReject()
    {
        var bill = CreateBill("BILL-PAID", SourceRegId, 10_000m);
        var source = HydrateClosed(SourceRegId, bill);
        var target = HydrateClosed(TargetRegId, CreateBill("BILL-T", TargetRegId, 10_000m));
        bill.FinalizeAllocation(PaymentType.ByKas, 10_000m, "kasir", "RO-SRC", TestDate);
        bill.Pay(PaymentType.ByKas, 1_000m, "PAY-001", TestDate);
        var request = MergeRequestModel.Create("MR-009", SourceRegId, TargetRegId);

        Action act = () => TataRekeningDomainTestHelper.MergeBillingService.Execute(request, source, target);

        act.Should().Throw<InvalidOperationException>().WithMessage("*pembayaran*");
    }

    #endregion

    #region Projection regeneration

    [Fact]
    public void P2_10_GivenAllocatedProjection_WhenClear_ThenShouldPreserveTransactionEvents()
    {
        var bill = CreateBill("BILL-PRJ", TargetRegId, 100_000m);
        var tataRekening = HydrateClosed(TargetRegId, bill);
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening, [BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);

        TataRekeningDomainTestHelper.ProjectionService.ClearProjection(tataRekening);

        bill.ListTransaction.Should().NotBeEmpty();
        bill.ListFinalization.Should().BeEmpty();
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeFalse();
    }

    [Fact]
    public void P2_11_GivenClearedProjection_WhenRegenerate_ThenShouldRestoreAllocation()
    {
        var bill = CreateBill("BILL-REGEN", TargetRegId, 100_000m);
        var tataRekening = HydrateClosed(TargetRegId, bill);
        tataRekening.CompleteFinancialVerification(DefaultPetugasVerif, TestDate);

        TataRekeningDomainTestHelper.ProjectionService.RegenerateProjection(
            tataRekening, [BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);

        bill.ListFinalization.Sum(x => x.Nilai).Should().Be(100_000m);
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();
    }

    #endregion

    #region Financial Verification

    [Fact]
    public void P2_12_GivenClosedWithoutPendingMerge_WhenVerify_ThenShouldBeValid()
    {
        var tataRekening = HydrateClosed(TargetRegId, CreateBill("BILL-V", TargetRegId, 10_000m));

        TataRekeningDomainTestHelper.VerificationService.Verify(
            tataRekening, DefaultPetugasVerif, TestDate, []);

        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.Valid);
    }

    [Fact]
    public void P2_13_GivenPendingMergeForReg_WhenVerify_ThenShouldReject()
    {
        var tataRekening = HydrateClosed(TargetRegId, CreateBill("BILL-V2", TargetRegId, 10_000m));
        var pending = MergeRequestModel.Create("MR-PEND", SourceRegId, TargetRegId);

        Action act = () => TataRekeningDomainTestHelper.VerificationService.Verify(
            tataRekening, DefaultPetugasVerif, TestDate, [pending]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Pending*");
    }

    [Fact]
    public void P2_14_GivenRequiresAdjustment_WhenResetVerification_ThenShouldBeNotVerified()
    {
        var tataRekening = HydrateClosed(TargetRegId, CreateBill("BILL-V3", TargetRegId, 10_000m));
        TataRekeningDomainTestHelper.VerificationService.RequireAdjustment(tataRekening);

        TataRekeningDomainTestHelper.VerificationService.ResetVerification(tataRekening);

        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);
    }

    #endregion

    #region Financial Adjustment

    [Fact]
    public void P2_15_GivenRequiresAdjustment_WhenWaive_ThenShouldReduceBillTotal()
    {
        var bill = CreateBill("BILL-WV", TargetRegId, 100_000m);
        var tataRekening = HydrateClosed(TargetRegId, bill);
        TataRekeningDomainTestHelper.VerificationService.RequireAdjustment(tataRekening);

        var result = TataRekeningDomainTestHelper.AdjustmentService.Apply(
            tataRekening,
            new FinancialAdjustmentRequest(
                FinancialAdjustmentTypeEnum.Waive, 20_000m, "Koreksi waive", bill.TrsBillingId),
            TestDate);

        result.RequiresReopen.Should().BeFalse();
        bill.Nilai.Total.Should().Be(100_000m);
        bill.FinancialTotal.Should().Be(80_000m);
        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);
    }

    [Fact]
    public void P2_16_GivenRequiresAdjustment_WhenSubsidy_ThenShouldReduceBillTotal()
    {
        var bill = CreateBill("BILL-SUB", TargetRegId, 50_000m);
        var tataRekening = HydrateClosed(TargetRegId, bill);
        tataRekening.RequireFinancialAdjustment();

        TataRekeningDomainTestHelper.AdjustmentService.Apply(
            tataRekening,
            new FinancialAdjustmentRequest(
                FinancialAdjustmentTypeEnum.Subsidy, 10_000m, "Subsidi RS",
                bill.TrsBillingId, PaymentType.ByKas),
            TestDate);

        bill.FinancialTotal.Should().Be(40_000m);
    }

    [Fact]
    public void P2_17_GivenRequiresAdjustment_WhenBillingCorrection_ThenShouldSetCorrectedTotal()
    {
        var bill = CreateBill("BILL-BC", TargetRegId, 100_000m);
        var tataRekening = HydrateClosed(TargetRegId, bill);
        tataRekening.RequireFinancialAdjustment();

        TataRekeningDomainTestHelper.AdjustmentService.Apply(
            tataRekening,
            new FinancialAdjustmentRequest(
                FinancialAdjustmentTypeEnum.BillingCorrection, 90_000m, "Koreksi nilai",
                bill.TrsBillingId),
            TestDate);

        bill.FinancialTotal.Should().Be(90_000m);
    }

    [Fact]
    public void P2_18_GivenRequiresAdjustment_WhenManualCharge_ThenShouldAddBillToSet()
    {
        var tataRekening = HydrateClosed(TargetRegId, CreateBill("BILL-EX", TargetRegId, 10_000m));
        tataRekening.RequireFinancialAdjustment();
        var manualBill = CreateBill("BILL-MAN", TargetRegId, 5_000m);

        TataRekeningDomainTestHelper.AdjustmentService.Apply(
            tataRekening,
            new FinancialAdjustmentRequest(
                FinancialAdjustmentTypeEnum.ManualCharge, 5_000m, "Manual charge",
                ManualChargeBill: manualBill),
            TestDate);

        tataRekening.ListTrsBill.Should().HaveCount(2);
        tataRekening.ListTrsBill.Should().Contain(b => b.TrsBillingId == "BILL-MAN");
    }

    [Fact]
    public void P2_19_GivenRequiresAdjustment_WhenMergeBillingCorrection_ThenShouldCorrectBill()
    {
        var bill = CreateBill("BILL-MBC", TargetRegId, 80_000m);
        var tataRekening = HydrateClosed(TargetRegId, bill);
        tataRekening.RequireFinancialAdjustment();

        TataRekeningDomainTestHelper.AdjustmentService.Apply(
            tataRekening,
            new FinancialAdjustmentRequest(
                FinancialAdjustmentTypeEnum.MergeBillingCorrection, 75_000m,
                "Koreksi pasca-merge", bill.TrsBillingId),
            TestDate);

        bill.FinancialTotal.Should().Be(75_000m);
    }

    [Fact]
    public void P2_20_GivenRequiresChargeSourceChange_WhenAdjust_ThenShouldRequireReopen()
    {
        var bill = CreateBill("BILL-RO", TargetRegId, 10_000m);
        var tataRekening = HydrateClosed(TargetRegId, bill);
        tataRekening.RequireFinancialAdjustment();

        var result = TataRekeningDomainTestHelper.AdjustmentService.Apply(
            tataRekening,
            new FinancialAdjustmentRequest(
                FinancialAdjustmentTypeEnum.BillingCorrection, 10_000m, "Perlu charge source",
                bill.TrsBillingId, RequiresChargeSourceChange: true),
            TestDate);

        result.RequiresReopen.Should().BeTrue();
        bill.Nilai.Total.Should().Be(10_000m);
    }

    [Fact]
    public void P2_21_GivenNotRequiresAdjustment_WhenAdjust_ThenShouldReject()
    {
        var tataRekening = HydrateClosed(TargetRegId, CreateBill("BILL-NO", TargetRegId, 10_000m));

        Action act = () => TataRekeningDomainTestHelper.AdjustmentService.Apply(
            tataRekening,
            new FinancialAdjustmentRequest(
                FinancialAdjustmentTypeEnum.Waive, 1_000m, "test", "BILL-NO"),
            TestDate);

        act.Should().Throw<InvalidOperationException>().WithMessage("*RequiresAdjustment*");
    }

    [Fact]
    public void P2_22_GivenAdjustedBill_WhenReVerifyAndAllocate_ThenShouldConserveTotals()
    {
        var bill = CreateBill("BILL-INV", TargetRegId, 100_000m);
        var tataRekening = HydrateClosed(TargetRegId, bill);
        tataRekening.RequireFinancialAdjustment();

        TataRekeningDomainTestHelper.AdjustmentService.Apply(
            tataRekening,
            new FinancialAdjustmentRequest(
                FinancialAdjustmentTypeEnum.Waive, 20_000m, "Waive", bill.TrsBillingId),
            TestDate);

        TataRekeningDomainTestHelper.VerificationService.Verify(
            tataRekening, DefaultPetugasVerif, TestDate, []);
        tataRekening.AllocateFinancialResponsibility(
            [BuildPayment(PaymentType.ByKas, 80_000m, 0m)]);

        bill.ListFinalization.Sum(x => x.Nilai).Should().Be(80_000m);
    }

    #endregion

    private const string DefaultPetugasVerif = TataRekeningDomainTestHelper.DefaultPetugasVerif;

    private static TataRekeningModel HydrateClosed(string regId, params TrsBillType[] bills)
    {
        var model = Hydrate(regId, TataRekeningStatusEnum.Opened, bills);
        model.Close();
        return model;
    }

    private static TataRekeningModel Hydrate(
        string regId, TataRekeningStatusEnum status, params TrsBillType[] listTrsBill) =>
        new(regId, status, TataRekeningFinalizationType.Default, [], listTrsBill);

    private static TataRekeningModel CreateLunas(string regId)
    {
        var model = Hydrate(regId, TataRekeningStatusEnum.Opened, CreateBill("BILL-L", regId, 10_000m));
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            model, [BuildPayment(PaymentType.ByKas, 10_000m, 0m)]);
        model.Pay([BuildPayment(PaymentType.ByKas, 10_000m, 0m)], "PAY-L", TestDate);
        return model;
    }

    private static TataRekeningPaymentType BuildPayment(PaymentType payment, decimal nilaiJasa, decimal nilaiObat) =>
        new(payment, nilaiJasa, nilaiObat, CoaType.Default);

    private static TrsBill2CoaType ValidPdpCoa => new(
        new CoaType("PPDP-01", ""),
        new CoaType("PDPT-01", ""),
        CoaType.Default,
        CoaType.Default,
        CoaType.Default,
        CoaType.Default);

    private static TrsBillType CreateBill(string billId, string regId, decimal amount)
    {
        var komponen = new TrsBill2KomponenType("KOMP-01", "Komponen Test");
        var trans = TrsBill2TransEventType.Create(
            0, komponen, TrsBillJenisBayarType.Pdp, amount,
            PpaType.Default.ToReff(), ValidPdpCoa);

        return new TrsBillType(
            billId, BillModulGroup.Jasa, TestDate,
            new RegReff(regId, "-", "-"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(amount, 0, 0, 0),
            new TrsBillKetType("Test", "", "REF", 1, ""),
            [trans], [], []);
    }
}
