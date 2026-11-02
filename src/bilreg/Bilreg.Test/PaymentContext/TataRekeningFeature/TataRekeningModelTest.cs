using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningModelTest
{
    private const string RegId = "REG-MODEL-001";
    private static readonly DateTime TestDate = new(2026, 6, 16, 10, 0, 0);

    [Fact]
    public void GivenClosedTataRekeningWithAllocations_WhenReOpen_ThenResetsAllocationVerificationAndFinalization()
    {
        // Arrange
        var bill = CreateBill("BILL-01", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)],
            "kasir-01",
            finalizationDate: TestDate);

        tataRekening.CancelFinalization();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Closed);

        // Act
        tataRekening.ReOpen();

        // Assert
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Opened);
        tataRekening.ListPayment.Should().BeEmpty();
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeFalse();
        tataRekening.SettlementInitiated.Should().BeFalse();
        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);
        tataRekening.FinancialVerificationInfo.Should().BeNull();
        tataRekening.FinalizationInfo.Should().Be(TataRekeningFinalizationType.Default);
        tataRekening.ListTrsBill.Single().ListFinalization.Should().BeEmpty();
    }

    [Fact]
    public void GivenReopenedAndReclosedBilling_WhenApplyRehydrationInference_ThenDoesNotInferValidOrAllocated()
    {
        // Arrange: simulated legacy/rehydrated model where bills might still have finalization events
        var bill = CreateBill("BILL-02", BillModulGroup.Jasa, 50_000m);
        bill.FinalizeAllocation(
            PaymentType.ByKas, 50_000m, "kasir-01", "RO00000001", TestDate);

        // Act: Hydrating Closed TataRekening with NotVerified and no payments
        var tataRekening = new TataRekeningModel(
            RegId,
            TataRekeningStatusEnum.Closed,
            TataRekeningFinalizationType.Default,
            [],
            [bill],
            financialVerificationStatus: FinancialVerificationStatusEnum.NotVerified,
            isFinancialResponsibilityAllocated: false);

        // Assert: Rehydration inference must NOT infer Valid or Allocated
        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeFalse();
    }

    [Fact]
    public void GivenFinalizedTataRekeningWithNotVerified_WhenApplyRehydrationInference_ThenInfersValidAndAllocated()
    {
        // Arrange: legacy finalized billing loaded without explicit verification status
        var bill = CreateBill("BILL-03", BillModulGroup.Jasa, 50_000m);

        // Act
        var tataRekening = new TataRekeningModel(
            RegId,
            TataRekeningStatusEnum.Finalized,
            new TataRekeningFinalizationType("kasir-01", TestDate),
            [],
            [bill],
            financialVerificationStatus: FinancialVerificationStatusEnum.NotVerified);

        // Assert
        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.Valid);
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();
    }

    [Fact]
    public void GivenFullCycle_FinalizeToCancelToReopenToClose_WhenCompleteFinancialVerification_ThenSucceedsWithoutException()
    {
        // 1. Arrange & Initial Cycle: Open -> Close -> Verify -> Allocate -> Finalize
        var bill = CreateBill("BILL-CYCLE", BillModulGroup.Jasa, 150_000m);
        var tataRekening = HydrateOpened(bill);

        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("petugas-awal", TestDate);
        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.Valid);

        tataRekening.AllocateFinancialResponsibility([BuildPayment(PaymentType.ByKas, 150_000m, 0m)]);
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();

        tataRekening.FinalizeFinancialResponsibility("kasir-awal", TestDate);
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);

        // 2. Cancel Finalization -> Reopen
        tataRekening.CancelFinalization();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Closed);

        tataRekening.ReOpen();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Opened);
        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeFalse();

        // 3. Re-close Billing (SOP-TR-02)
        tataRekening.Close();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Closed);
        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);

        // 4. Simulate Rehydration from DB after Close
        var rehydrated = new TataRekeningModel(
            tataRekening.RegId,
            tataRekening.Status,
            tataRekening.FinalizationInfo,
            tataRekening.ListPayment,
            tataRekening.ListTrsBill,
            tataRekening.FinancialVerificationStatus,
            tataRekening.FinancialVerificationInfo,
            tataRekening.IsFinancialResponsibilityAllocated,
            tataRekening.SettlementInitiated,
            tataRekening.Version);

        rehydrated.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);

        // 5. Complete Financial Verification (SOP-TR-04) - Must NOT throw InvalidOperationException
        var act = () => rehydrated.CompleteFinancialVerification("petugas-ulang", TestDate.AddHours(2));

        act.Should().NotThrow<InvalidOperationException>();
        rehydrated.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.Valid);
        rehydrated.FinancialVerificationInfo.Should().NotBeNull();
        rehydrated.FinancialVerificationInfo!.PetugasVerif.Should().Be("petugas-ulang");
    }

    private static TataRekeningModel HydrateOpened(params TrsBillType[] listTrsBill) =>
        new(RegId, TataRekeningStatusEnum.Opened, TataRekeningFinalizationType.Default, [], listTrsBill);

    private static TataRekeningPaymentType BuildPayment(PaymentType payment, decimal nilaiJasa, decimal nilaiObat) =>
        new(payment, nilaiJasa, nilaiObat, CoaType.Default);

    private static TrsBillType CreateBill(string billId, BillModulGroup modulGroup, decimal amount) =>
        TataRekeningTestDataBuilder.CreateBill(billId, RegId, amount) with { ModulGroup = modulGroup };
}
