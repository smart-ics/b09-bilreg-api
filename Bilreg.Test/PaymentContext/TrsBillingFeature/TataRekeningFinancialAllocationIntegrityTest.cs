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

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

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

    private static readonly DateTime DischargeDate = new(2026, 6, 16, 10, 0, 0);
    private static readonly DateTime PaymentDate = new(2026, 6, 16, 11, 0, 0);

    #region Test Group 1 — Single Provider → Multiple Bills

    [Fact]
    public void TG01_GivenSingleProviderAndMultipleBills_WhenDischarge_ThenProviderAndBillTotalsConserved()
    {
        var billA = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 600m));
        var billB = CreateBillWithComponents("BILL-B", BillModulGroup.Jasa, ("KOMP-B", "Bill-B", 400m));
        var tataRekening = HydrateOpened(billA, billB);
        tataRekening.Close();

        tataRekening.Discharge(
            [BuildPayment(Bpjs, 1_000m, 0m)],
            "kasir-01",
            DischargeDate);

        SumDischargeByProvider(billA, Bpjs).Should().Be(600m);
        SumDischargeByProvider(billB, Bpjs).Should().Be(400m);

        FinancialConservationAudit.AssertDischargeConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 2 — Multiple Providers → Multiple Bills

    [Fact]
    public void TG02_GivenMultipleProvidersAndMultipleBills_WhenDischarge_ThenProviderAndBillTotalsConserved()
    {
        var billA = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 600m));
        var billB = CreateBillWithComponents("BILL-B", BillModulGroup.Jasa, ("KOMP-B", "Bill-B", 400m));
        var tataRekening = HydrateOpened(billA, billB);
        tataRekening.Close();

        tataRekening.Discharge(
            [
                BuildPayment(Bpjs, 700m, 0m),
                BuildPayment(Kas, 300m, 0m)
            ],
            "kasir-01",
            DischargeDate);

        FinancialConservationAudit.ProviderTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.BillTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.DischargeComponentTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.AssertDischargeConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 3 — Bill → Component Proportional Split

    [Fact]
    public void TG03_GivenBillWithComponents_WhenSingleProviderDischarge_ThenComponentTotalsMatchBill()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("DOCTOR", "Doctor", 500m),
            ("HOSPITAL", "Hospital", 300m),
            ("BHP", "BHP", 200m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        tataRekening.Discharge(
            [BuildPayment(Bpjs, 1_000m, 0m)],
            "kasir-01",
            DischargeDate);

        SumDischargeByComponent(bill, "DOCTOR", Bpjs).Should().Be(500m);
        SumDischargeByComponent(bill, "HOSPITAL", Bpjs).Should().Be(300m);
        SumDischargeByComponent(bill, "BHP", Bpjs).Should().Be(200m);

        FinancialConservationAudit.DischargeComponentTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.AssertDischargeConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 4 — Provider → Bill → Component Full Chain

    [Fact]
    public void TG04_GivenFullChain_WhenDischarge_ThenProportionalSplitAcrossProvidersAndComponents()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("DOCTOR", "Doctor", 500m),
            ("HOSPITAL", "Hospital", 300m),
            ("BHP", "BHP", 200m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        tataRekening.Discharge(
            [
                BuildPayment(Bpjs, 700m, 0m),
                BuildPayment(Kas, 300m, 0m)
            ],
            "kasir-01",
            DischargeDate);

        SumDischargeByComponent(bill, "DOCTOR", Bpjs).Should().Be(350m);
        SumDischargeByComponent(bill, "DOCTOR", Kas).Should().Be(150m);
        SumDischargeByComponent(bill, "HOSPITAL", Bpjs).Should().Be(210m);
        SumDischargeByComponent(bill, "HOSPITAL", Kas).Should().Be(90m);
        SumDischargeByComponent(bill, "BHP", Bpjs).Should().Be(140m);
        SumDischargeByComponent(bill, "BHP", Kas).Should().Be(60m);

        SumDischargeByProvider(bill, Bpjs).Should().Be(700m);
        SumDischargeByProvider(bill, Kas).Should().Be(300m);

        FinancialConservationAudit.AssertDischargeConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 5 — Multiple Bills Multiple Components

    [Fact]
    public void TG05_GivenMultipleBillsWithComponents_WhenDischarge_ThenAllLevelsConserved()
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
        tataRekening.Close();

        tataRekening.Discharge(
            [
                BuildPayment(Bpjs, 700m, 0m),
                BuildPayment(Kas, 300m, 0m)
            ],
            "kasir-01",
            DischargeDate);

        FinancialConservationAudit.ProviderTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.BillTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.DischargeComponentTotal(tataRekening).Should().Be(1_000m);
        FinancialConservationAudit.AssertDischargeConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 6 — Payment Allocation Uses Discharge Allocation

    [Fact]
    public void TG06_GivenDischarge_WhenPay_ThenPaymentComponentsDeriveFromDischargeNotTransaction()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("DOCTOR", "Doctor", 500m),
            ("HOSPITAL", "Hospital", 300m),
            ("BHP", "BHP", 200m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();
        tataRekening.Discharge(
            [
                BuildPayment(Bpjs, 700m, 0m),
                BuildPayment(Kas, 300m, 0m)
            ],
            "kasir-01",
            DischargeDate);

        tataRekening.Pay(
            [BuildPayment(Kas, 300m, 0m)],
            "PAY-001",
            PaymentDate);

        var dischargeKomponenKeys = bill.ListDischarge
            .Select(d => (d.Komponen.BillKompId, d.JenisBayar))
            .ToHashSet();
        var transactionKomponenIds = bill.ListTransaction
            .Select(t => t.Komponen.BillKompId)
            .ToHashSet();

        foreach (var payment in bill.ListPayment)
        {
            dischargeKomponenKeys.Should().Contain(
                (payment.Komponen.BillKompId, payment.JenisBayar),
                "payment component must reference a discharge component, not be independently derived from transaction");

            var matchingDischarge = bill.ListDischarge.Single(d =>
                d.Komponen.BillKompId == payment.Komponen.BillKompId &&
                d.JenisBayar == payment.JenisBayar);

            payment.Komponen.BillKompId.Should().Be(matchingDischarge.Komponen.BillKompId);
            payment.JenisBayar.Should().Be(matchingDischarge.JenisBayar);
        }

        // Payment proportions follow discharge proportions for the paid provider, not raw transaction weights.
        var kasDischarges = bill.ListDischarge.Where(d => MatchesProvider(d.JenisBayar, Kas)).ToList();
        var kasPayments = bill.ListPayment.Where(p => MatchesProvider(p.JenisBayar, Kas)).ToList();
        kasPayments.Sum(p => p.Nilai).Should().Be(300m);
        foreach (var discharge in kasDischarges)
        {
            var paid = kasPayments
                .Where(p => p.Komponen.BillKompId == discharge.Komponen.BillKompId)
                .Sum(p => p.Nilai);
            paid.Should().Be(discharge.Nilai);
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
        tataRekening.Close();
        tataRekening.Discharge(
            [BuildPayment(Kas, 1_000m, 0m)],
            "kasir-01",
            DischargeDate);

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
    public void TG08_GivenRoundingComponents_WhenDischarge_ThenNoCentLostOrDuplicated()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("C1", "Component-1", 33.33m),
            ("C2", "Component-2", 33.33m),
            ("C3", "Component-3", 33.34m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        tataRekening.Discharge(
            [BuildPayment(Bpjs, 100m, 0m)],
            "kasir-01",
            DischargeDate);

        FinancialConservationAudit.DischargeComponentTotal(tataRekening).Should().Be(100m);
        bill.ListDischarge.Sum(d => d.Nilai).Should().Be(100m);
        FinancialConservationAudit.AssertDischargeConservation(tataRekening, 100m);
    }

    #endregion

    #region Test Group 9 — Very Small Amount Stress Test

    [Fact]
    public void TG09_GivenVerySmallAmounts_WhenDischarge_ThenFinancialConservationHolds()
    {
        var bill = CreateBillWithComponents(
            "BILL-A",
            BillModulGroup.Jasa,
            ("C1", "Component-1", 0.01m),
            ("C2", "Component-2", 0.01m),
            ("C3", "Component-3", 0.01m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        tataRekening.Discharge(
            [BuildPayment(Bpjs, 0.03m, 0m)],
            "kasir-01",
            DischargeDate);

        FinancialConservationAudit.DischargeComponentTotal(tataRekening).Should().Be(0.03m);
        FinancialConservationAudit.AssertDischargeConservation(tataRekening, 0.03m);
    }

    #endregion

    #region Test Group 10 — JASA / OBAT Partition Integrity

    [Fact]
    public void TG10_GivenJasaOnlyProvider_WhenDischarge_ThenAllocationNeverAppearsOnObatBills()
    {
        var jasaBill = CreateBillWithComponents("BILL-JASA", BillModulGroup.Jasa, ("KOMP-J", "Jasa", 700m));
        var obatBill = CreateBillWithComponents("BILL-OBAT", BillModulGroup.Obat, ("KOMP-O", "Obat", 300m));
        var tataRekening = HydrateOpened(jasaBill, obatBill);
        tataRekening.Close();

        tataRekening.Discharge(
            [
                BuildPayment(BpjsJasa, 700m, 0m),
                BuildPayment(Kas, 0m, 300m)
            ],
            "kasir-01",
            DischargeDate);

        SumDischargeByProvider(jasaBill, BpjsJasa).Should().Be(700m);
        jasaBill.ListDischarge.Should().NotBeEmpty();
        obatBill.ListDischarge.Where(d => MatchesProvider(d.JenisBayar, BpjsJasa)).Should().BeEmpty();
        SumDischargeByProvider(obatBill, Kas).Should().Be(300m);

        FinancialConservationAudit.AssertDischargeConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 11 — Discharge Invariant

    [Fact]
    public void TG11_GivenUnderAllocation_WhenDischarge_ThenShouldReject()
    {
        var bill = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 1_000m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        Action act = () => tataRekening.Discharge(
            [BuildPayment(Bpjs, 999m, 0m)],
            "kasir-01",
            DischargeDate);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak sama*");
    }

    [Fact]
    public void TG11_GivenOverAllocation_WhenDischarge_ThenShouldReject()
    {
        var bill = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 1_000m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        Action act = () => tataRekening.Discharge(
            [BuildPayment(Bpjs, 1_001m, 0m)],
            "kasir-01",
            DischargeDate);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak sama*");
    }

    [Fact]
    public void TG11_GivenExactAllocation_WhenDischarge_ThenShouldAccept()
    {
        var bill = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 1_000m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        tataRekening.Discharge(
            [BuildPayment(Bpjs, 1_000m, 0m)],
            "kasir-01",
            DischargeDate);

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);
        FinancialConservationAudit.AssertDischargeConservation(tataRekening, 1_000m);
    }

    #endregion

    #region Test Group 12 — Financial Conservation Audit Helpers

    [Fact]
    public void TG12_FinancialConservationAudit_ShouldDetectMismatchWhenTotalsDiverge()
    {
        var bill = CreateBillWithComponents("BILL-A", BillModulGroup.Jasa, ("KOMP-A", "Bill-A", 1_000m));
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();
        tataRekening.Discharge(
            [BuildPayment(Bpjs, 1_000m, 0m)],
            "kasir-01",
            DischargeDate);

        Action act = () => FinancialConservationAudit.AssertDischargeConservation(tataRekening, 999m);

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

        public static decimal DischargeComponentTotal(TataRekeningModel tataRekening) =>
            tataRekening.ListTrsBill.SelectMany(b => b.ListDischarge).Sum(d => d.Nilai);

        public static decimal DischargeBillTotal(TataRekeningModel tataRekening) =>
            tataRekening.ListTrsBill.Sum(b => b.ListDischarge.Sum(d => d.Nilai));

        public static decimal PaymentComponentTotal(TataRekeningModel tataRekening) =>
            tataRekening.ListTrsBill.SelectMany(b => b.ListPayment).Sum(p => p.Nilai);

        public static void AssertDischargeConservation(TataRekeningModel tataRekening, decimal expectedTotal)
        {
            var providerTotal = ProviderTotal(tataRekening);
            var billTotal = BillTotal(tataRekening);
            var dischargeComponentTotal = DischargeComponentTotal(tataRekening);
            var dischargeBillTotal = DischargeBillTotal(tataRekening);

            if (providerTotal != expectedTotal ||
                billTotal != expectedTotal ||
                dischargeComponentTotal != expectedTotal ||
                dischargeBillTotal != expectedTotal ||
                providerTotal != billTotal ||
                providerTotal != dischargeComponentTotal)
            {
                throw new Exception(
                    $"Financial conservation violated at discharge: " +
                    $"expected={expectedTotal}, provider={providerTotal}, bill={billTotal}, " +
                    $"dischargeComponent={dischargeComponentTotal}, dischargeBill={dischargeBillTotal}");
            }

            foreach (var bill in tataRekening.ListTrsBill)
            {
                var billDischarged = bill.ListDischarge.Sum(d => d.Nilai);
                if (billDischarged != bill.Nilai.Total)
                    throw new Exception(
                        $"Financial conservation violated on bill '{bill.TrsBillingId}': " +
                        $"discharged={billDischarged}, billTotal={bill.Nilai.Total}");
            }
        }

        public static void AssertPaymentConservation(TataRekeningModel tataRekening, decimal expectedPaymentTotal)
        {
            var paymentTotal = PaymentComponentTotal(tataRekening);
            var dischargeTotal = DischargeComponentTotal(tataRekening);

            if (paymentTotal > dischargeTotal)
                throw new Exception(
                    $"Financial conservation violated at payment: payment={paymentTotal} exceeds discharge={dischargeTotal}");

            if (paymentTotal != expectedPaymentTotal)
                throw new Exception(
                    $"Financial conservation violated at payment: expected={expectedPaymentTotal}, actual={paymentTotal}");
        }

        public static void AssertFullConservation(TataRekeningModel tataRekening, decimal expectedTotal)
        {
            AssertDischargeConservation(tataRekening, expectedTotal);

            var paymentTotal = PaymentComponentTotal(tataRekening);
            if (paymentTotal != expectedTotal)
                throw new Exception(
                    $"Financial conservation violated at full settlement: payment={paymentTotal}, expected={expectedTotal}");

            if (paymentTotal != DischargeComponentTotal(tataRekening))
                throw new Exception(
                    "Financial conservation violated: payment components do not equal discharge components.");
        }
    }

    #endregion

    #region Query Helpers

    private static decimal SumDischargeByProvider(TrsBillType bill, PaymentType provider) =>
        bill.ListDischarge
            .Where(d => MatchesProvider(d.JenisBayar, provider))
            .Sum(d => d.Nilai);

    private static decimal SumDischargeByComponent(TrsBillType bill, string kompId, PaymentType provider) =>
        bill.ListDischarge
            .Where(d => d.Komponen.BillKompId == kompId && MatchesProvider(d.JenisBayar, provider))
            .Sum(d => d.Nilai);

    private static decimal Outstanding(TataRekeningModel tataRekening) =>
        tataRekening.ListTrsBill.Sum(b =>
            b.ListDischarge.Sum(d => d.Nilai) - b.ListPayment.Sum(p => p.Nilai));

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
        new(REG_ID, TataRekeningStatusEnum.Opened, TataRekeningDischargeType.Default, [], listTrsBill);

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
