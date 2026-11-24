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

    #region TD-01 & TD-02 Invariant & PaymentType Tests

    [Fact]
    public void PaymentType_ByKas_PaymentName_ShouldBeKasirBayarSendiri()
    {
        PaymentType.ByKas.PaymentName.Should().Be("Kasir (Bayar Sendiri)");
    }

    [Fact]
    public void GivenTotalHutangDanJaminanExceedsKurangBayar_WhenAllocate_ThenThrowsArgumentException()
    {
        // Tagihan = 100_000, DPU = 40_000, DPK = 0, BYKAS = -10_000 => netDpu = 30_000 => maxHutangDanJaminan = 70_000
        // Hutang Pribadi (BYPRI) = 80_000 => totalHutangDanJaminan = 80_000 > 70_000
        var bill = CreateBill("BILL-INV-01", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByDpu, 40_000m, 0m),
            BuildPayment(PaymentType.ByPri, 80_000m, 0m),
            BuildPayment(PaymentType.ByKas, -10_000m, 0m)
        ]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh melebihi sisa Kurang Bayar*");
    }

    [Fact]
    public void GivenJaminanExceedsKurangBayar_WhenAllocate_ThenThrowsArgumentException()
    {
        // Tagihan = 100_000, DPU = 50_000, DPK = 20_000, BYKAS = -10_000 => netDpu = 40_000 => maxHutangDanJaminan = 40_000
        // Jaminan (BPJS) = 50_000 => exceeds 40_000
        var bill = CreateBill("BILL-INV-02", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var bpjs = new PaymentType("BPJS", "BPJS Kesehatan", true);
        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByDpu, 50_000m, 0m),
            BuildPayment(PaymentType.ByDpk, 20_000m, 0m),
            BuildPayment(bpjs, 50_000m, 0m),
            BuildPayment(PaymentType.ByKas, -10_000m, 0m)
        ]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh melebihi sisa Kurang Bayar*");
    }

    [Fact]
    public void GivenSampleRG01378265_WhenFullDebtWithCashierRefundOfDeposit_ThenAllocationSucceeds()
    {
        // Sample Registrasi RG01378265:
        // Tagihan = 60_000, Deposit Umum = 25_000
        // Alokasi: Hutang Pribadi (BYPRI) = 60_000, Deposit Umum (BYDPU) = 25_000, Kasir (BYKAS) = -25_000
        // netDpuApplied = Math.Max(0, 25_000 - 25_000) = 0
        // maxHutangDanJaminan = 60_000 - 0 = 60_000
        // totalHutangDanJaminan (60_000) <= maxHutangDanJaminan (60_000) => Valid!
        var bill = CreateBill("BILL-RG01378265", BillModulGroup.Jasa, 60_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByPri, 60_000m, 0m),
            BuildPayment(PaymentType.ByDpu, 25_000m, 0m),
            BuildPayment(PaymentType.ByKas, -25_000m, 0m)
        ]);

        act.Should().NotThrow();
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();
        tataRekening.ListPayment.Should().HaveCount(3);
    }

    [Fact]
    public void GivenSampleRG01378265_WhenHutangExceedsMaxHutangDanJaminan_ThenThrowsArgumentException()
    {
        // Sample Registrasi RG01378265:
        // Tagihan = 60_000, Deposit Umum = 25_000, Kasir (BYKAS) = -25_000 => maxHutangDanJaminan = 60_000
        // Hutang Pribadi (BYPRI) = 65_000 > maxHutangDanJaminan (60_000)
        var bill = CreateBill("BILL-RG01378265-EXCEED", BillModulGroup.Jasa, 60_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByPri, 65_000m, 0m),
            BuildPayment(PaymentType.ByDpu, 25_000m, 0m),
            BuildPayment(PaymentType.ByKas, -25_000m, 0m)
        ]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh melebihi sisa Kurang Bayar*");
    }

    [Fact]
    public void GivenTotalDpkExceedsTotalTagihan_WhenAllocate_ThenThrowsArgumentException()
    {
        // Tagihan = 100_000, DPK = 110_000
        var bill = CreateBill("BILL-INV-03", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByDpk, 110_000m, 0m),
            BuildPayment(PaymentType.ByKas, -10_000m, 0m)
        ]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh melebihi total tagihan*");
    }

    [Fact]
    public void GivenNegativeByKasWithoutDpu_WhenAllocate_ThenThrowsArgumentException()
    {
        // Tagihan = 100_000, no DPU, BYVCH = 110_000, BYKAS = -10_000 (byKasTotal = -10_000 < 0 without DPU)
        var bill = CreateBill("BILL-INV-04", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByVch, 110_000m, 0m),
            BuildPayment(PaymentType.ByKas, -10_000m, 0m)
        ]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*hanya diizinkan jika terdapat Deposit Umum*");
    }

    [Fact]
    public void GivenNegativeByKasComponentWithoutDpu_WhenAllocate_ThenThrowsArgumentException()
    {
        // Tagihan = 100_000 (Jasa 50k, Obat 50k), no DPU
        // BYKAS has NilaiJasa = -10k, NilaiObat = 110k (byKasTotal = 100k >= 0, but component < 0)
        var billJasa = CreateBill("BILL-INV-05A", BillModulGroup.Jasa, 50_000m);
        var billObat = CreateBill("BILL-INV-05B", BillModulGroup.Obat, 50_000m);
        var tataRekening = HydrateOpened(billJasa, billObat);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByKas, -10_000m, 110_000m)
        ]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*hanya diizinkan jika terdapat Deposit Umum*");
    }

    [Fact]
    public void GivenNegativeByKasExceedsDpu_WhenAllocate_ThenThrowsArgumentException()
    {
        // Tagihan = 100_000, DPU = 20_000 => Kurang Bayar = 80_000
        // BYPRI = 80_000 (valid <= Kurang Bayar), BYVCH = 30_000, BYKAS = -30_000 (|-30_000| > 20_000 DPU)
        var bill = CreateBill("BILL-INV-06", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByDpu, 20_000m, 0m),
            BuildPayment(PaymentType.ByPri, 80_000m, 0m),
            BuildPayment(PaymentType.ByVch, 30_000m, 0m),
            BuildPayment(PaymentType.ByKas, -30_000m, 0m)
        ]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh melebihi total Deposit Umum*");
    }

    [Fact]
    public void GivenNegativeByKasWithinDpu_WhenAllocate_ThenSucceeds()
    {
        // Tagihan = 100_000, DPU = 150_000, BYKAS = -50_000
        // |-50_000| <= 150_000, Kurang Bayar = max(0, 100k - 150k) = 0, HutangDanJaminan = 0
        var bill = CreateBill("BILL-INV-07", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByDpu, 150_000m, 0m),
            BuildPayment(PaymentType.ByKas, -50_000m, 0m)
        ]);

        act.Should().NotThrow();
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();
    }

    [Fact]
    public void GivenDepositUmum10000AndKasirMinus9000_WhenAllocate_ThenValid()
    {
        // Scenario P1-S03: Deposit Umum Rp 10.000, Kasir -Rp 9.000 => VALID (|-9.000| <= 10.000)
        // Tagihan = 1_000 (Jasa 1_000), DPU = 10_000, BYKAS = -9_000 => Total Alokasi = 1_000
        var bill = CreateBill("BILL-ABS-VALID", BillModulGroup.Jasa, 1_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByDpu, 10_000m, 0m),
            BuildPayment(PaymentType.ByKas, -9_000m, 0m)
        ]);

        act.Should().NotThrow();
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();
    }

    [Fact]
    public void GivenDepositUmum10000AndKasirMinus11000_WhenAllocate_ThenInvalidThrowsArgumentException()
    {
        // Scenario P1-S03: Deposit Umum Rp 10.000, Kasir -Rp 11.000 => INVALID (|-11.000| > 10.000)
        // DPU = 10_000, BYKAS = -11_000, BYVCH = 2_000 (Total = 1_000)
        var bill = CreateBill("BILL-ABS-INVALID", BillModulGroup.Jasa, 1_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByDpu, 10_000m, 0m),
            BuildPayment(PaymentType.ByKas, -11_000m, 0m),
            BuildPayment(PaymentType.ByVch, 2_000m, 0m)
        ]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Nilai alokasi negatif Kasir (11000) tidak boleh melebihi total Deposit Umum (10000)*");
    }

    [Fact]
    public void GivenExactHutangDanJaminanEqualsKurangBayar_WhenAllocate_ThenSucceeds()
    {
        // Tagihan = 100_000, DPU = 30_000, DPK = 20_000 => Kurang Bayar = 50_000
        // BPJS = 30_000, BYPRI = 20_000 => totalHutangDanJaminan = 50_000 == 50_000
        var bill = CreateBill("BILL-INV-08", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        var bpjs = new PaymentType("BPJS", "BPJS Kesehatan", true);
        var act = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByDpu, 30_000m, 0m),
            BuildPayment(PaymentType.ByDpk, 20_000m, 0m),
            BuildPayment(bpjs, 30_000m, 0m),
            BuildPayment(PaymentType.ByPri, 20_000m, 0m)
        ]);

        act.Should().NotThrow();
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();
    }

    #endregion

    #region TD-06 Multi-Bill Multi-Payer Terminal Absorption Tests

    [Fact]
    public void GivenScenarioRG01378279_MultiBillMultiPayer_WhenAllocateAndFinalize_ThenFinalizesWithoutDiscrepancyAndMaintainsInvariants()
    {
        // Skenario RG01378279 (TD-06 Acceptance Condition 7):
        // 3 bills on Jasa: Bill1 = 10,000, Bill2 = 100,000, Bill3 = 100,000 (Total Jasa = 210,000)
        // 3 payers: ADMEDIKA (100,000 Jasa), PLN (60,000 Jasa), Kasir (50,000 Jasa)
        var bill1 = CreateBill("BILL-RG-01", BillModulGroup.Jasa, 10_000m);
        var bill2 = CreateBill("BILL-RG-02", BillModulGroup.Jasa, 100_000m);
        var bill3 = CreateBill("BILL-RG-03", BillModulGroup.Jasa, 100_000m);
        var tataRekening = HydrateOpened(bill1, bill2, bill3);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);

        var admedika = new PaymentType("ADM00", "ADMEDIKA", true);
        var pln = new PaymentType("PL501", "PLN UID JATENG", true);
        var kas = PaymentType.ByKas;

        // Act 1: Allocate
        var actAllocate = () => tataRekening.AllocateFinancialResponsibility([
            BuildPayment(admedika, 100_000m, 0m),
            BuildPayment(pln, 60_000m, 0m),
            BuildPayment(kas, 50_000m, 0m)
        ]);
        actAllocate.Should().NotThrow();
        tataRekening.IsFinancialResponsibilityAllocated.Should().BeTrue();

        // Act 2: Finalize
        var actFinalize = () => tataRekening.FinalizeFinancialResponsibility("kasir-01", TestDate);
        actFinalize.Should().NotThrow();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);

        // Assert: Every bill's finalization sum equals its FinancialTotal exactly
        bill1.ListFinalization.Sum(x => x.Nilai).Should().Be(bill1.FinancialTotal);
        bill2.ListFinalization.Sum(x => x.Nilai).Should().Be(bill2.FinancialTotal);
        bill3.ListFinalization.Sum(x => x.Nilai).Should().Be(bill3.FinancialTotal);

        // Assert: Detailed distribution according to TD-06
        // ADMEDIKA (i=0): 4,761.90, 47,619.05, 47,619.05 (Sum = 100,000.00)
        bill1.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "ADM00").Nilai.Should().Be(4_761.90m);
        bill2.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "ADM00").Nilai.Should().Be(47_619.05m);
        bill3.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "ADM00").Nilai.Should().Be(47_619.05m);

        // PLN (i=1): 2,857.14, 28,571.43, 28,571.43 (Sum = 60,000.00)
        bill1.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "PL501").Nilai.Should().Be(2_857.14m);
        bill2.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "PL501").Nilai.Should().Be(28_571.43m);
        bill3.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "PL501").Nilai.Should().Be(28_571.43m);

        // Kasir (i=2, Terminal payer absorbs exact remaining capacity):
        // Bill1: 10,000 - (4,761.90 + 2,857.14) = 2,380.96 (previously 2,380.95 with old rounding causing deficit)
        // Bill2: 100,000 - (47,619.05 + 28,571.43) = 23,809.52
        // Bill3: 100,000 - (47,619.05 + 28,571.43) = 23,809.52
        // Total Kasir across bills: 2,380.96 + 23,809.52 + 23,809.52 = 50,000.00
        bill1.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(2_380.96m);
        bill2.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(23_809.52m);
        bill3.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(23_809.52m);

        var totalKasir = new[] { bill1, bill2, bill3 }
            .SelectMany(b => b.ListFinalization)
            .Where(f => f.JenisBayar.JenisBayarId == "KAS")
            .Sum(f => f.Nilai);
        totalKasir.Should().Be(50_000.00m);
    }

    [Fact]
    public void GivenRepeatingDecimalScenario_MultiBillMultiPayer_WhenAllocateAndFinalize_ThenAllBillsAndPayersAreExactlyBalanced()
    {
        // 3 bills of 10,000 each in Jasa (Total = 30,000)
        // 3 payers: BPJS (10,000), ASURANSI (10,000), Kasir (10,000)
        // (10,000 / 30,000 = 1/3 = 0.333333... repeating decimal)
        var bill1 = CreateBill("BILL-REP-01", BillModulGroup.Jasa, 10_000m);
        var bill2 = CreateBill("BILL-REP-02", BillModulGroup.Jasa, 10_000m);
        var bill3 = CreateBill("BILL-REP-03", BillModulGroup.Jasa, 10_000m);
        var tataRekening = HydrateOpened(bill1, bill2, bill3);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);

        var bpjs = new PaymentType("BPJS", "BPJS Kesehatan", true);
        var asuransi = new PaymentType("ASU01", "Asuransi Swasta", true);
        var kas = PaymentType.ByKas;

        tataRekening.AllocateFinancialResponsibility([
            BuildPayment(bpjs, 10_000m, 0m),
            BuildPayment(asuransi, 10_000m, 0m),
            BuildPayment(kas, 10_000m, 0m)
        ]);

        var actFinalize = () => tataRekening.FinalizeFinancialResponsibility("kasir-01", TestDate);
        actFinalize.Should().NotThrow();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);

        var allBills = new[] { bill1, bill2, bill3 };

        // Verify each bill is exactly 100% satisfied
        foreach (var bill in allBills)
        {
            bill.ListFinalization.Sum(x => x.Nilai).Should().Be(bill.FinancialTotal);
        }

        // Verify each payer's total across all bills matches assigned amount (10,000)
        allBills.SelectMany(b => b.ListFinalization)
            .Where(f => f.JenisBayar.JenisBayarId == "BPJS")
            .Sum(f => f.Nilai).Should().Be(10_000m);

        allBills.SelectMany(b => b.ListFinalization)
            .Where(f => f.JenisBayar.JenisBayarId == "ASU01")
            .Sum(f => f.Nilai).Should().Be(10_000m);

        allBills.SelectMany(b => b.ListFinalization)
            .Where(f => f.JenisBayar.JenisBayarId == "KAS")
            .Sum(f => f.Nilai).Should().Be(10_000m);

        // Verify detailed amounts
        // BPJS (i=0): 3333.33, 3333.33, 3333.34
        bill1.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "BPJS").Nilai.Should().Be(3_333.33m);
        bill2.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "BPJS").Nilai.Should().Be(3_333.33m);
        bill3.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "BPJS").Nilai.Should().Be(3_333.34m);

        // ASU01 (i=1): 3333.33, 3333.33, 3333.34
        bill1.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "ASU01").Nilai.Should().Be(3_333.33m);
        bill2.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "ASU01").Nilai.Should().Be(3_333.33m);
        bill3.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "ASU01").Nilai.Should().Be(3_333.34m);

        // KAS (i=2, Terminal payer absorbs remaining capacity):
        // Bill1: 10,000 - 6,666.66 = 3,333.34
        // Bill2: 10,000 - 6,666.66 = 3,333.34
        // Bill3: 10,000 - 6,666.68 = 3,333.32
        bill1.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(3_333.34m);
        bill2.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(3_333.34m);
        bill3.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(3_333.32m);
    }

    [Fact]
    public void GivenMixedJasaAndObatMultiBillMultiPayer_WhenAllocateAndFinalize_ThenBothModuleGroupsFinalizeExactly()
    {
        // 2 bills Jasa: 30,000 + 70,000 = 100,000
        // 2 bills Obat: 40,000 + 60,000 = 100,000
        // Total = 200,000
        var billJasa1 = CreateBill("BILL-MIX-J1", BillModulGroup.Jasa, 30_000m);
        var billJasa2 = CreateBill("BILL-MIX-J2", BillModulGroup.Jasa, 70_000m);
        var billObat1 = CreateBill("BILL-MIX-O1", BillModulGroup.Obat, 40_000m);
        var billObat2 = CreateBill("BILL-MIX-O2", BillModulGroup.Obat, 60_000m);

        var tataRekening = HydrateOpened(billJasa1, billJasa2, billObat1, billObat2);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);

        var bpjs = new PaymentType("BPJS", "BPJS Kesehatan", true);
        var asuransi = new PaymentType("ASU02", "Asuransi Lain", true);
        var kas = PaymentType.ByKas;

        // Payers:
        // BPJS: Jasa = 40,000, Obat = 50,000
        // ASU02: Jasa = 35,000, Obat = 25,000
        // Kasir: Jasa = 25,000, Obat = 25,000
        tataRekening.AllocateFinancialResponsibility([
            BuildPayment(bpjs, 40_000m, 50_000m),
            BuildPayment(asuransi, 35_000m, 25_000m),
            BuildPayment(kas, 25_000m, 25_000m)
        ]);

        var actFinalize = () => tataRekening.FinalizeFinancialResponsibility("kasir-01", TestDate);
        actFinalize.Should().NotThrow();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);

        var allBills = new[] { billJasa1, billJasa2, billObat1, billObat2 };

        // Verify each bill's finalization sum equals its FinancialTotal
        foreach (var bill in allBills)
        {
            bill.ListFinalization.Sum(x => x.Nilai).Should().Be(bill.FinancialTotal);
        }

        // Verify payer sums across all bills
        allBills.SelectMany(b => b.ListFinalization)
            .Where(f => f.JenisBayar.JenisBayarId == "BPJS")
            .Sum(f => f.Nilai).Should().Be(90_000m);

        allBills.SelectMany(b => b.ListFinalization)
            .Where(f => f.JenisBayar.JenisBayarId == "ASU02")
            .Sum(f => f.Nilai).Should().Be(60_000m);

        allBills.SelectMany(b => b.ListFinalization)
            .Where(f => f.JenisBayar.JenisBayarId == "KAS")
            .Sum(f => f.Nilai).Should().Be(50_000m);
    }

    [Fact]
    public void GivenSinglePayerMultiBill_WhenAllocateAndFinalize_ThenEachBillAbsorbsExactCapacity()
    {
        // 3 bills Jasa: 15,000, 35,000, 50,000 (Total = 100,000)
        // 1 payer: Kasir (100,000)
        var bill1 = CreateBill("BILL-SP-01", BillModulGroup.Jasa, 15_000m);
        var bill2 = CreateBill("BILL-SP-02", BillModulGroup.Jasa, 35_000m);
        var bill3 = CreateBill("BILL-SP-03", BillModulGroup.Jasa, 50_000m);

        var tataRekening = HydrateOpened(bill1, bill2, bill3);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);

        tataRekening.AllocateFinancialResponsibility([
            BuildPayment(PaymentType.ByKas, 100_000m, 0m)
        ]);

        var actFinalize = () => tataRekening.FinalizeFinancialResponsibility("kasir-01", TestDate);
        actFinalize.Should().NotThrow();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);

        bill1.ListFinalization.Single().Nilai.Should().Be(15_000m);
        bill2.ListFinalization.Single().Nilai.Should().Be(35_000m);
        bill3.ListFinalization.Single().Nilai.Should().Be(50_000m);
        bill1.ListFinalization.Sum(x => x.Nilai).Should().Be(bill1.FinancialTotal);
        bill2.ListFinalization.Sum(x => x.Nilai).Should().Be(bill2.FinancialTotal);
        bill3.ListFinalization.Sum(x => x.Nilai).Should().Be(bill3.FinancialTotal);
    }

    [Fact]
    public void GivenSingleBillMultiPayer_WhenAllocateAndFinalize_ThenBillAbsorbsAllPayersExactly()
    {
        // 1 bill Jasa: 100,000
        // 3 payers: BPJS (50,000), ASU (30,000), Kasir (20,000)
        var bill = CreateBill("BILL-SB-01", BillModulGroup.Jasa, 100_000m);

        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("verif-01", TestDate);

        var bpjs = new PaymentType("BPJS", "BPJS Kesehatan", true);
        var asuransi = new PaymentType("ASU03", "Asuransi Lain", true);

        tataRekening.AllocateFinancialResponsibility([
            BuildPayment(bpjs, 50_000m, 0m),
            BuildPayment(asuransi, 30_000m, 0m),
            BuildPayment(PaymentType.ByKas, 20_000m, 0m)
        ]);

        var actFinalize = () => tataRekening.FinalizeFinancialResponsibility("kasir-01", TestDate);
        actFinalize.Should().NotThrow();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);

        bill.ListFinalization.Should().HaveCount(3);
        bill.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "BPJS").Nilai.Should().Be(50_000m);
        bill.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "ASU03").Nilai.Should().Be(30_000m);
        bill.ListFinalization.Single(x => x.JenisBayar.JenisBayarId == "KAS").Nilai.Should().Be(20_000m);
        bill.ListFinalization.Sum(x => x.Nilai).Should().Be(bill.FinancialTotal);
    }

    #endregion
}
