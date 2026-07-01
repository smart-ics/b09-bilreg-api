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

/// <summary>
/// Validates financial proportional allocation across Provider → Bill → Component chain.
/// Every test proves the Financial Conservation Law: Σ Input = Σ Output at each level.
/// </summary>
public class TataRekeningFinancialAllocationIntegrityTest
{
    private const string REG_ID = "REG-ALLOC-001";

    private static readonly PaymentType Bpjs = new("BPJS", "BPJS", true);
    private static readonly PaymentType BpjsJasa = new("BPJS-JASA", "BPJS Jasa", true);
    private static readonly PaymentType Kas = PaymentType.ByKas;

    private static readonly DateTime FinalizationDate = new(2026, 6, 16, 10, 0, 0);
    private static readonly DateTime PaymentDate = new(2026, 6, 16, 11, 0, 0);

    #region Test Group 1 — Single Provider → Multiple Bills

    [Fact]
    public void TG01_GivenSingleProviderAndMultipleBills_WhenFinalizeFinancialResponsibility_ThenProviderAndBillTotalsConserved()
    {
        var billA = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 600m));
        var billB = CreateBillWithComponents("BILL-B", BillModulGroup.Jasa, ("KOMP-B", "Bill-B", 400m));
        var tataRekening = HydrateOpened(billA, billB);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [BuildPayment(Bpjs, 1_000m, 0m)],
            "kasir-01",
            finalizationDate: FinalizationDate);

        SumFinalizationByProvider(billA, Bpjs).Should().Be(600m);
        SumFinalizationByProvider(billB, Bpjs).Should().Be(400m);

        FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 2 — Multiple Providers → Multiple Bills

    [Fact]
    public void TG02_GivenMultipleProvidersAndMultipleBills_WhenFinalizeFinancialResponsibility_ThenProviderAndBillTotalsConserved()
    {
        var billA = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 600m));
        var billB = CreateBillWithComponents("BILL-B", BillModulGroup.Jasa, ("KOMP-B", "Bill-B", 400m));
        var tataRekening = HydrateOpened(billA, billB);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [
                BuildPayment(Bpjs, 700m, 0m),
                BuildPayment(Kas, 300m, 0m)
            ],
            "kasir-01",
            finalizationDate: FinalizationDate);

        FinancialConservationAudit.ProviderTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.BillTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.FinalizationComponentTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 3 — Bill → Component Proportional Split

    [Fact]
    public void TG03_GivenBillWithComponents_WhenSingleProviderFinalization_ThenComponentTotalsMatchBill()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("DOCTOR", "Doctor", 500m),
            ("HOSPITAL", "Hospital", 300m),
            ("BHP", "BHP", 200m));
        var tataRekening = HydrateOpened(bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [BuildPayment(Bpjs, 1_000m, 0m)],
            "kasir-01",
            finalizationDate: FinalizationDate);

        SumFinalizationByComponent(bill, "DOCTOR", Bpjs).Should().Be(500m);
        SumFinalizationByComponent(bill, "HOSPITAL", Bpjs).Should().Be(300m);
        SumFinalizationByComponent(bill, "BHP", Bpjs).Should().Be(200m);

        FinancialConservationAudit.FinalizationComponentTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 4 — Provider → Bill → Component Full Chain

    [Fact]
    public void TG04_GivenFullChain_WhenFinalizeFinancialResponsibility_ThenProportionalSplitAcrossProvidersAndComponents()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("DOCTOR", "Doctor", 500m),
            ("HOSPITAL", "Hospital", 300m),
            ("BHP", "BHP", 200m));
        var tataRekening = HydrateOpened(bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [
                BuildPayment(Bpjs, 700m, 0m),
                BuildPayment(Kas, 300m, 0m)
            ],
            "kasir-01",
            finalizationDate: FinalizationDate);

        SumFinalizationByComponent(bill, "DOCTOR", Bpjs).Should().Be(350m);
        SumFinalizationByComponent(bill, "DOCTOR", Kas).Should().Be(150m);
        SumFinalizationByComponent(bill, "HOSPITAL", Bpjs).Should().Be(210m);
        SumFinalizationByComponent(bill, "HOSPITAL", Kas).Should().Be(90m);
        SumFinalizationByComponent(bill, "BHP", Bpjs).Should().Be(140m);
        SumFinalizationByComponent(bill, "BHP", Kas).Should().Be(60m);

        SumFinalizationByProvider(bill, Bpjs).Should().Be(700m);
        SumFinalizationByProvider(bill, Kas).Should().Be(300m);

        FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 5 — Multiple Bills Multiple Components

    [Fact]
    public void TG05_GivenMultipleBillsWithComponents_WhenFinalizeFinancialResponsibility_ThenAllLevelsConserved()
    {
        var billA = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("DOCTOR", "Doctor", 300m),
            ("HOSPITAL", "Hospital", 200m),
            ("BHP", "BHP", 100m));
        var billB = CreateBillWithComponents(
            "BILL-B",
            BillModulGroup.Jasa,
            ("DOCTOR", "Doctor", 200m),
            ("HOSPITAL", "Hospital", 100m),
            ("BHP", "BHP", 100m));
        var tataRekening = HydrateOpened(billA, billB);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [
                BuildPayment(Bpjs, 700m, 0m),
                BuildPayment(Kas, 300m, 0m)
            ],
            "kasir-01",
            finalizationDate: FinalizationDate);

        FinancialConservationAudit.ProviderTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.BillTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.FinalizationComponentTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 6 — Payment Allocation Uses Finalization Allocation

    [Fact]
    public void TG06_GivenFinalization_WhenPay_ThenPaymentComponentsDeriveFromFinalizationNotTransaction()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("DOCTOR", "Doctor", 500m),
            ("HOSPITAL", "Hospital", 300m),
            ("BHP", "BHP", 200m));
        var tataRekening = HydrateOpened(bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [
                BuildPayment(Bpjs, 700m, 0m),
                BuildPayment(Kas, 300m, 0m)
            ],
            "kasir-01",
            finalizationDate: FinalizationDate);

        tataRekening.Pay(
            [BuildPayment(Kas, 300m, 0m)],
            "PAY-001",
            PaymentDate);

        var finalizationKomponenKeys = bill.ListFinalization
            .Select(d => (d.Komponen.BillKompId, d.JenisBayar))
            .ToHashSet();
        var transactionKomponenIds = bill.ListTransaction
            .Select(t => t.Komponen.BillKompId)
            .ToHashSet();

        foreach (var payment in bill.ListPayment)
        {
            finalizationKomponenKeys.Should().Contain(
                (payment.Komponen.BillKompId, payment.JenisBayar),
                "payment component must reference a finalization component, not be independently derived from transaction");

            var matchingFinalization = bill.ListFinalization.Single(d =>
                d.Komponen.BillKompId == payment.Komponen.BillKompId &&
                d.JenisBayar == payment.JenisBayar);

            payment.Komponen.BillKompId.Should().Be(matchingFinalization.Komponen.BillKompId);
            payment.JenisBayar.Should().Be(matchingFinalization.JenisBayar);
        }

        // Payment proportions follow finalization proportions for the paid provider, not raw transaction weights.
        var kasFinalizations = bill.ListFinalization.Where(d => MatchesProvider(d.JenisBayar, Kas)).ToList();
        var kasPayments = bill.ListPayment.Where(p => MatchesProvider(p.JenisBayar, Kas)).ToList();
        kasPayments.Sum(p => p.Nilai).Should().Be(300m);
        foreach (var finalization in kasFinalizations)
        {
            var paid = kasPayments
                .Where(p => p.Komponen.BillKompId == finalization.Komponen.BillKompId)
                .Sum(p => p.Nilai);
            paid.Should().Be(finalization.Nilai);
        }

        transactionKomponenIds.Should().NotBeEmpty();
        FinancialConservationAudit.AssertPaymentConservation(tataRekening, 300m);
    }

    #endregion

    #region Test Group 7 — Multiple Payments

    [Fact]
    public void TG07_GivenMultiplePartialPayments_WhenFullyPaid_ThenComponentsConservedAndLunas()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("DOCTOR", "Doctor", 500m),
            ("HOSPITAL", "Hospital", 300m),
            ("BHP", "BHP", 200m));
        var tataRekening = HydrateOpened(bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [BuildPayment(Kas, 1_000m, 0m)],
            "kasir-01",
            finalizationDate: FinalizationDate);

        tataRekening.Pay([BuildPayment(Kas, 300m, 0m)], "PAY-001", PaymentDate);
        tataRekening.Pay([BuildPayment(Kas, 500m, 0m)], "PAY-002", PaymentDate.AddHours(1));
        tataRekening.Pay([BuildPayment(Kas, 200m, 0m)], "PAY-003", PaymentDate.AddHours(2));

        FinancialConservationAudit.PaymentComponentTotal(tataRekening).Should().Be(1_000m);
        Outstanding(tataRekening).Should().Be(0m);
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Lunas);

        FinancialConservationAudit.AssertFullConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 8 — Rounding Stress Test

    [Fact]
    public void TG08_GivenRoundingComponents_WhenFinalizeFinancialResponsibility_ThenNoCentLostOrDuplicated()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("C1", "Component-1", 33.33m),
            ("C2", "Component-2", 33.33m),
            ("C3", "Component-3", 33.34m));
        var tataRekening = HydrateOpened(bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [BuildPayment(Bpjs, 100m, 0m)],
            "kasir-01",
            finalizationDate: FinalizationDate);

        FinancialConservationAudit.FinalizationComponentTotal(tataRekening).Should().Be(100m);
        bill.ListFinalization.Sum(d => d.Nilai).Should().Be(100m);
        FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 100m);
    }

    #endregion

    #region Test Group 9 — Very Small Amount Stress Test

    [Fact]
    public void TG09_GivenVerySmallAmounts_WhenFinalizeFinancialResponsibility_ThenFinancialConservationHolds()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("C1", "Component-1", 0.01m),
            ("C2", "Component-2", 0.01m),
            ("C3", "Component-3", 0.01m));
        var tataRekening = HydrateOpened(bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [BuildPayment(Bpjs, 0.03m, 0m)],
            "kasir-01",
            finalizationDate: FinalizationDate);

        FinancialConservationAudit.FinalizationComponentTotal(tataRekening).Should().Be(0.03m);
        FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 0.03m);
    }

    #endregion

    #region Test Group 10 — JASA / OBAT Partition Integrity

    [Fact]
    public void TG10_GivenJasaOnlyProvider_WhenFinalizeFinancialResponsibility_ThenAllocationNeverAppearsOnObatBills()
    {
        var jasaBill = CreateBillWithComponents("BILL-JASA", BillModulGroup.Jasa, ("KOMP-J", "Jasa", 700m));
        var obatBill = CreateBillWithComponents("BILL-OBAT", BillModulGroup.Obat, ("KOMP-O", "Obat", 300m));
        var tataRekening = HydrateOpened(jasaBill, obatBill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [
                BuildPayment(BpjsJasa, 700m, 0m),
                BuildPayment(Kas, 0m, 300m)
            ],
            "kasir-01",
            finalizationDate: FinalizationDate);

        SumFinalizationByProvider(jasaBill, BpjsJasa).Should().Be(700m);
        jasaBill.ListFinalization.Should().NotBeEmpty();
        obatBill.ListFinalization.Where(d => MatchesProvider(d.JenisBayar, BpjsJasa)).Should().BeEmpty();
        SumFinalizationByProvider(obatBill, Kas).Should().Be(300m);

        FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 11 — Finalization Invariant

    [Fact]
    public void TG11_GivenUnderAllocation_WhenAllocateFinancialResponsibility_ThenShouldReject()
    {
        var bill = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 1_000m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("kasir-01", FinalizationDate);

        Action act = () => tataRekening.AllocateFinancialResponsibility(
            [BuildPayment(Bpjs, 999m, 0m)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak sama*");
    }

    [Fact]
    public void TG11_GivenOverAllocation_WhenAllocateFinancialResponsibility_ThenShouldReject()
    {
        var bill = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 1_000m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification("kasir-01", FinalizationDate);

        Action act = () => tataRekening.AllocateFinancialResponsibility(
            [BuildPayment(Bpjs, 1_001m, 0m)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak sama*");
    }

    [Fact]
    public void TG11_GivenExactAllocation_WhenFinalizeFinancialResponsibility_ThenShouldAccept()
    {
        var bill = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 1_000m));
        var tataRekening = HydrateOpened(bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [BuildPayment(Bpjs, 1_000m, 0m)],
            "kasir-01",
            finalizationDate: FinalizationDate);

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);
        FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 12 — Financial Conservation Audit Helpers

    [Fact]
    public void TG12_FinancialConservationAudit_ShouldDetectMismatchWhenTotalsDiverge()
    {
        var bill = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 1_000m));
        var tataRekening = HydrateOpened(bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [BuildPayment(Bpjs, 1_000m, 0m)],
            "kasir-01",
            finalizationDate: FinalizationDate);

        Action act = () => FinancialConservationAudit.AssertFinalizationConservation(tataRekening, 999m);

        act.Should().Throw<Exception>()
            .WithMessage("*Financial conservation violated*");
    }

    #endregion

    #region Financial Conservation Audit (reusable across all tests)

    private static class FinancialConservationAudit
    {
        public static decimal ProviderTotal(TataRekeningModel tataRekening) =>
            tataRekening.ListPayment.Sum(x => x.NilaiJasa + x.NilaiObat);

        public static decimal BillTotal(TataRekeningModel tataRekening) =>
            tataRekening.ListTrsBill.Sum(b => b.Nilai.Total);

        public static decimal FinalizationComponentTotal(TataRekeningModel tataRekening) =>
            tataRekening.ListTrsBill.SelectMany(b => b.ListFinalization).Sum(d => d.Nilai);

        public static decimal FinalizationBillTotal(TataRekeningModel tataRekening) =>
            tataRekening.ListTrsBill.Sum(b => b.ListFinalization.Sum(d => d.Nilai));

        public static decimal PaymentComponentTotal(TataRekeningModel tataRekening) =>
            tataRekening.ListTrsBill.SelectMany(b => b.ListPayment).Sum(p => p.Nilai);

        public static void AssertFinalizationConservation(TataRekeningModel tataRekening, decimal expectedTotal)
        {
            var providerTotal = ProviderTotal(tataRekening);
            var billTotal = BillTotal(tataRekening);
            var finalizationComponentTotal = FinalizationComponentTotal(tataRekening);
            var finalizationBillTotal = FinalizationBillTotal(tataRekening);

            if (providerTotal != expectedTotal ||
                billTotal != expectedTotal ||
                finalizationComponentTotal != expectedTotal ||
                finalizationBillTotal != expectedTotal ||
                providerTotal != billTotal ||
                providerTotal != finalizationComponentTotal)
            {
                throw new Exception(
                    $"Financial conservation violated at finalization: " +
                    $"expected={expectedTotal}, provider={providerTotal}, bill={billTotal}, " +
                    $"finalizationComponent={finalizationComponentTotal}, finalizationBill={finalizationBillTotal}");
            }

            foreach (var bill in tataRekening.ListTrsBill)
            {
                var billFinalized = bill.ListFinalization.Sum(d => d.Nilai);
                if (billFinalized != bill.Nilai.Total)
                    throw new Exception(
                        $"Financial conservation violated on bill '{bill.TrsBillingId}': " +
                        $"finalized={billFinalized}, billTotal={bill.Nilai.Total}");
            }
        }

        public static void AssertPaymentConservation(TataRekeningModel tataRekening, decimal expectedPaymentTotal)
        {
            var paymentTotal = PaymentComponentTotal(tataRekening);
            var finalizationTotal = FinalizationComponentTotal(tataRekening);

            if (paymentTotal > finalizationTotal)
                throw new Exception(
                    $"Financial conservation violated at payment: payment={paymentTotal} exceeds finalization={finalizationTotal}");

            if (paymentTotal != expectedPaymentTotal)
                throw new Exception(
                    $"Financial conservation violated at payment: expected={expectedPaymentTotal}, actual={paymentTotal}");
        }

        public static void AssertFullConservation(TataRekeningModel tataRekening, decimal expectedTotal)
        {
            AssertFinalizationConservation(tataRekening, expectedTotal);

            var paymentTotal = PaymentComponentTotal(tataRekening);
            if (paymentTotal != expectedTotal)
                throw new Exception(
                    $"Financial conservation violated at full settlement: payment={paymentTotal}, expected={expectedTotal}");

            if (paymentTotal != FinalizationComponentTotal(tataRekening))
                throw new Exception(
                    "Financial conservation violated: payment components do not equal finalization components.");
        }
    }

    #endregion

    #region Query Helpers

    private static decimal SumFinalizationByProvider(TrsBillType bill, PaymentType provider) =>
        bill.ListFinalization
            .Where(d => MatchesProvider(d.JenisBayar, provider))
            .Sum(d => d.Nilai);

    private static decimal SumFinalizationByComponent(TrsBillType bill, string kompId, PaymentType provider) =>
        bill.ListFinalization
            .Where(d => d.Komponen.BillKompId == kompId && MatchesProvider(d.JenisBayar, provider))
            .Sum(d => d.Nilai);

    private static decimal Outstanding(TataRekeningModel tataRekening) =>
        tataRekening.ListTrsBill.Sum(b =>
            b.ListFinalization.Sum(d => d.Nilai) - b.ListPayment.Sum(p => p.Nilai));

    private static bool MatchesProvider(TrsBillJenisBayarType jenisBayar, PaymentType payment)
    {
        if (payment == PaymentType.ByKas)
            return jenisBayar == TrsBillJenisBayarType.Kas;

        if (payment.IsTipeJaminan && jenisBayar.IsTipeJaminan)
            return string.Equals(jenisBayar.JenisBayarId, payment.PaymentId, StringComparison.Ordinal);

        return false;
    }

    #endregion

    #region Bill Builders

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

    private static TrsBillType CreateBillWithComponents(
        string billId,
        BillModulGroup modulGroup,
        params (string KompId, string KompName, decimal Amount)[] components)
    {
        var transactions = components
            .Select((c, i) => TrsBill2TransEventType.Create(
                i,
                new TrsBill2KomponenType(c.KompId, c.KompName),
                TrsBillJenisBayarType.Pdp,
                c.Amount,
                PpaType.Default.ToReff(),
                ValidPdpCoa))
            .ToList();

        var total = components.Sum(c => c.Amount);

        return new TrsBillType(
            billId,
            modulGroup,
            new DateTime(2026, 6, 16),
            new RegReff(REG_ID, "-", "-"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(total, 0, 0, 0),
            new TrsBillKetType("Test", "", "REF", 1, ""),
            transactions,
            [],
            []);
    }

    #endregion
}
