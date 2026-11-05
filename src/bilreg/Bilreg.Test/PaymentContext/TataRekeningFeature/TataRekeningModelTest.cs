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

    [Fact]
    public void GivenClosedTataRekeningWithoutVerification_WhenAllocateFinancialResponsibility_ThenSucceedsDirectly()
    {
        // TD-01: Relaxed 2-step invariant - allocation permitted directly upon Closed status
        var bill = CreateBill("BILL-RELAX-01", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        tataRekening.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);

        // Act: allocating without prior CompleteFinancialVerification
        var act = () => tataRekening.AllocateFinancialResponsibility([BuildPayment(PaymentType.ByKas, 100_000m, 0m)]);

        // Assert: Succeeds directly without throwing
        act.Should().NotThrow();
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();
        bill.ListFinalization.Should().NotBeEmpty();
        bill.ListFinalization.Sum(x => x.Nilai).Should().Be(100_000m);
    }

    [Fact]
    public void GivenMultipleBillsWithFractionalTotals_WhenAllocateFinalization_ThenProportionalSplitRoundsToTwoDecimalsAndAbsorbsRemainder()
    {
        // TD-02: 3 bills with 100 total each in Jasa (Total = 300)
        // ModulAllocation = 100m.
        // Bill 1: Round(100 * 100 / 300, 2) = 33.33m
        // Bill 2: Round(100 * 100 / 300, 2) = 33.33m
        // Bill 3: 100 - (33.33 + 33.33) = 33.34m
        var bill1 = CreateBill("BILL-FRACT-01", BillModulGroup.Jasa, 100m);
        var bill2 = CreateBill("BILL-FRACT-02", BillModulGroup.Jasa, 100m);
        var bill3 = CreateBill("BILL-FRACT-03", BillModulGroup.Jasa, 100m);
        var tataRekening = HydrateOpened(bill1, bill2, bill3);
        tataRekening.Close();

        var bpjs = new PaymentType("BPJS", "BPJS", true);
        var kas = PaymentType.ByKas;

        // Allocate 100 to BPJS, 200 to Kasir
        tataRekening.AllocateFinancialResponsibility([
            BuildPayment(bpjs, 100m, 0m),
            BuildPayment(kas, 200m, 0m)
        ]);

        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();

        // Check Bill 1, 2, 3 finalizations
        var bill1Final = bill1.ListFinalization.ToList();
        var bill2Final = bill2.ListFinalization.ToList();
        var bill3Final = bill3.ListFinalization.ToList();

        // BPJS shares: 33.33 + 33.33 + 33.34 = 100
        bill1Final.First(x => x.JenisBayar.JenisBayarId == "BPJS").Nilai.Should().Be(33.33m);
        bill2Final.First(x => x.JenisBayar.JenisBayarId == "BPJS").Nilai.Should().Be(33.33m);
        bill3Final.First(x => x.JenisBayar.JenisBayarId == "BPJS").Nilai.Should().Be(33.34m);

        // Kas shares: Round(200 * 100 / 300, 2) = 66.67m, last absorbs: 200 - 133.34 = 66.66m
        bill1Final.First(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(66.67m);
        bill2Final.First(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(66.67m);
        bill3Final.First(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(66.66m);

        // Finalize should succeed cleanly without AssertFinalizationComplete discrepancy
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);
        var actFinalize = () => tataRekening.FinalizeFinancialResponsibility("kasir-01", TestDate);
        actFinalize.Should().NotThrow();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);
    }

    private static TrsBillType CreateBill(string billId, BillModulGroup modulGroup, decimal amount) =>
        TataRekeningTestDataBuilder.CreateBill(billId, RegId, amount) with { ModulGroup = modulGroup };

    private static TrsBillType CreateAdminBill(string billId, decimal amount, DateTime? date = null)
    {
        var dt = date ?? new DateTime(3000, 1, 1, 0, 0, 0);
        var bill = CreateBill(billId, BillModulGroup.Jasa, amount);
        return new TrsBillType(
            billId,
            bill.ModulGroup,
            dt,
            bill.Reg,
            bill.Layanan,
            bill.Kelas,
            bill.AuditInfo,
            bill.RekapCetak,
            bill.Nilai,
            bill.Keterangan,
            bill.ListTransaction,
            bill.ListFinalization,
            bill.ListPayment);
    }

    [Fact]
    public void GivenTataRekeningWithAdminBill_WhenFinalizeWithAdminBillIds_ThenAdminBillTglTrsUpdatedToFinalizationDate()
    {
        // Arrange
        var normalBillDate = new DateTime(2026, 6, 16, 8, 0, 0);
        var normalBill = new TrsBillType(
            "BILL-NORMAL",
            BillModulGroup.Jasa,
            normalBillDate,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).Reg,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).Layanan,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).Kelas,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).AuditInfo,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).RekapCetak,
            new TrsBillNilaiType(100_000m, 0, 0, 0),
            new TrsBillKetType("Tindakan", "", "REF", 1, ""),
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).ListTransaction,
            [],
            []);

        var adminBill = CreateAdminBill("BL-01", 25_000m, new DateTime(3000, 1, 1, 0, 0, 0));
        var tataRekening = HydrateOpened(normalBill, adminBill);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);
        tataRekening.AllocateFinancialResponsibility([BuildPayment(PaymentType.ByKas, 125_000m, 0m)]);

        var finalizationDate = new DateTime(2026, 11, 4, 15, 0, 0);

        // Act
        tataRekening.FinalizeFinancialResponsibility("kasir-01", finalizationDate, ["BL-01"]);

        // Assert
        adminBill.TglTrs.Should().Be(finalizationDate);
        normalBill.TglTrs.Should().Be(normalBillDate);
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);
    }

    [Fact]
    public void GivenTataRekeningWithSentinelAdminBill_WhenFinalizeWithoutAdminBillIds_ThenFallbackSentinelDateUpdatedToFinalizationDate()
    {
        // Arrange
        var normalBillDate = new DateTime(2026, 6, 16, 8, 0, 0);
        var normalBill = new TrsBillType(
            "BILL-NORMAL",
            BillModulGroup.Jasa,
            normalBillDate,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).Reg,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).Layanan,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).Kelas,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).AuditInfo,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).RekapCetak,
            new TrsBillNilaiType(100_000m, 0, 0, 0),
            new TrsBillKetType("Tindakan", "", "REF", 1, ""),
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).ListTransaction,
            [],
            []);

        var adminBill = CreateAdminBill("BL-02", 25_000m, new DateTime(3000, 1, 1, 0, 0, 0));
        var tataRekening = HydrateOpened(normalBill, adminBill);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);
        tataRekening.AllocateFinancialResponsibility([BuildPayment(PaymentType.ByKas, 125_000m, 0m)]);

        var finalizationDate = new DateTime(2026, 11, 4, 15, 0, 0);

        // Act - without passing adminBillIds (null default)
        tataRekening.FinalizeFinancialResponsibility("kasir-01", finalizationDate);

        // Assert - fallback by Year >= 2999 applies to adminBill
        adminBill.TglTrs.Should().Be(finalizationDate);
        normalBill.TglTrs.Should().Be(normalBillDate);
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);
    }

    [Fact]
    public void GivenFinalizedTataRekeningWithAdminBill_WhenCancelFinalizationWithAdminBillIds_ThenAdminBillTglTrsRevertsToSentinel()
    {
        // Arrange
        var normalBillDate = new DateTime(2026, 6, 16, 8, 0, 0);
        var normalBill = new TrsBillType(
            "BILL-NORMAL",
            BillModulGroup.Jasa,
            normalBillDate,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).Reg,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).Layanan,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).Kelas,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).AuditInfo,
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).RekapCetak,
            new TrsBillNilaiType(100_000m, 0, 0, 0),
            new TrsBillKetType("Tindakan", "", "REF", 1, ""),
            CreateBill("BILL-NORMAL", BillModulGroup.Jasa, 100_000m).ListTransaction,
            [],
            []);

        var adminBill = CreateAdminBill("BL-01", 25_000m, new DateTime(3000, 1, 1, 0, 0, 0));
        var tataRekening = HydrateOpened(normalBill, adminBill);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);
        tataRekening.AllocateFinancialResponsibility([BuildPayment(PaymentType.ByKas, 125_000m, 0m)]);

        var finalizationDate = new DateTime(2026, 11, 4, 15, 0, 0);
        tataRekening.FinalizeFinancialResponsibility("kasir-01", finalizationDate, ["BL-01"]);
        adminBill.TglTrs.Should().Be(finalizationDate);

        // Act
        tataRekening.CancelFinalization(["BL-01"]);

        // Assert
        adminBill.TglTrs.Should().Be(new DateTime(3000, 1, 1, 0, 0, 0));
        normalBill.TglTrs.Should().Be(normalBillDate);
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Closed);
    }

    [Fact]
    public void GivenFinalizedTataRekeningWithAdminBill_WhenRevertAdministrativeBillsDate_ThenAdminBillTglTrsRevertsToSentinel()
    {
        // Arrange
        var adminBill = CreateAdminBill("BL-01", 25_000m, new DateTime(3000, 1, 1, 0, 0, 0));
        var tataRekening = HydrateOpened(adminBill);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);
        tataRekening.AllocateFinancialResponsibility([BuildPayment(PaymentType.ByKas, 25_000m, 0m)]);

        var finalizationDate = new DateTime(2026, 11, 4, 15, 0, 0);
        tataRekening.FinalizeFinancialResponsibility("kasir-01", finalizationDate, ["BL-01"]);
        adminBill.TglTrs.Should().Be(finalizationDate);

        // Act
        tataRekening.RevertAdministrativeBillsDate(["BL-01"]);

        // Assert
        adminBill.TglTrs.Should().Be(new DateTime(3000, 1, 1, 0, 0, 0));
    }
}
