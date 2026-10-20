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
using Xunit;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillTypeTest
{
    private static readonly TrsBill2CoaType ValidPdpCoa = new(
        new CoaType("PPDP-01", ""),
        new CoaType("PDPT-01", ""),
        CoaType.Default,
        CoaType.Default,
        CoaType.Default,
        CoaType.Default);

    private static TrsBillType CreateBillWithTransComponents(string billId, params (string KompKd, decimal Nilai, int NoUrut)[] components)
    {
        var transList = components.Select(c =>
            TrsBill2TransEventType.Create(
                c.NoUrut,
                new TrsBill2KomponenType(c.KompKd, $"Komp {c.KompKd}"),
                TrsBillJenisBayarType.Pdp,
                c.Nilai,
                PpaType.Default.ToReff(),
                ValidPdpCoa)).ToList();

        var total = components.Sum(c => c.Nilai);

        return new TrsBillType(
            billId,
            BillModulGroup.Jasa,
            new DateTime(2026, 6, 16),
            new RegReff("REG-01", "-", "-"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(total, 0, 0, 0),
            new TrsBillKetType("Test", "", "REF", 1, ""),
            transList,
            [],
            []);
    }

    [Fact]
    public void GivenBillWithTransactionComponents_WhenFinalizeAllocation_ThenSequenceNumberContinuesFromTransactionComponents()
    {
        // 2 trans components with NoUrut 0 and 1
        var bill = CreateBillWithTransComponents("BILL-01", ("KOMP-A", 60_000m, 0), ("KOMP-B", 40_000m, 1));
        var initialDate = new DateTime(2026, 6, 16, 8, 0, 0);

        bill.FinalizeAllocation(PaymentType.ByKas, 100_000m, "kasir-01", "RO-001", initialDate);

        bill.ListFinalization.Should().HaveCount(2);
        var finalizations = bill.ListFinalization.ToList();
        finalizations.Should().AllSatisfy(f => f.NoUrut.Should().NotBe(0));
        finalizations[0].NoUrut.Should().Be(2);
        finalizations[1].NoUrut.Should().Be(3);
    }

    [Fact]
    public void GivenBillWithInitialTransactionComponents_WhenProportionalAllocationFinalized_ThenNoUrutIsNotZeroAndContinuesSequence()
    {
        // 3 trans components with NoUrut 0, 1, and 2
        var bill = CreateBillWithTransComponents(
            "BILL-PROP-01",
            ("KOMP-A", 50_000m, 0),
            ("KOMP-B", 30_000m, 1),
            ("KOMP-C", 20_000m, 2));
        var initialDate = new DateTime(2026, 6, 16, 8, 30, 0);

        bill.FinalizeAllocation(PaymentType.ByKas, 100_000m, "kasir-01", "RO-001", initialDate);

        bill.ListFinalization.Should().HaveCount(3);
        var finalizations = bill.ListFinalization.ToList();

        // fn_no_urut must not be 0 and must continue the sequence from max(trans.NoUrut) = 2
        finalizations.Should().AllSatisfy(f => f.NoUrut.Should().BeGreaterThan(0));
        finalizations[0].NoUrut.Should().Be(3);
        finalizations[0].Nilai.Should().Be(50_000m);
        finalizations[1].NoUrut.Should().Be(4);
        finalizations[1].Nilai.Should().Be(30_000m);
        finalizations[2].NoUrut.Should().Be(5);
        finalizations[2].Nilai.Should().Be(20_000m);
    }

    [Fact]
    public void GivenBillWithMultiPayerAllocation_WhenFinalizeAllocationAgain_ThenSequenceNumberContinuesFromPreviousFinalization()
    {
        // 2 trans components with NoUrut 0 and 1
        var bill = CreateBillWithTransComponents("BILL-02", ("KOMP-A", 60_000m, 0), ("KOMP-B", 40_000m, 1));
        var initialDate = new DateTime(2026, 6, 16, 8, 0, 0);

        var bpjsPayment = new PaymentType("BPJS", "BPJS", true);
        bill.FinalizeAllocation(bpjsPayment, 70_000m, "kasir-01", "RO-001", initialDate);
        bill.FinalizeAllocation(PaymentType.ByKas, 30_000m, "kasir-01", "RO-001", initialDate);

        bill.ListFinalization.Should().HaveCount(4);
        var finalizations = bill.ListFinalization.ToList();
        finalizations[0].NoUrut.Should().Be(2);
        finalizations[1].NoUrut.Should().Be(3);
        finalizations[2].NoUrut.Should().Be(4);
        finalizations[3].NoUrut.Should().Be(5);
    }

    [Fact]
    public void GivenBillWithFinalizationEvents_WhenUpdateFinalizationTimestamp_ThenAllEventsUpdated()
    {
        var bill = CreateBillWithTransComponents("BILL-03", ("KOMP-A", 50_000m, 0), ("KOMP-B", 50_000m, 1));
        var initialDate = new DateTime(2026, 6, 16, 8, 0, 0);
        bill.FinalizeAllocation(PaymentType.ByKas, 100_000m, "petugas-awal", "RO-001", initialDate);

        var finalDate = new DateTime(2026, 6, 16, 10, 45, 30);
        bill.UpdateFinalizationTimestamp(finalDate, "verifikator-01");

        bill.ListFinalization.Should().AllSatisfy(e =>
        {
            e.TglBayar.Should().Be(finalDate);
            e.PetugasKasir.Should().Be("verifikator-01");
        });
    }

    [Fact]
    public void GivenBill_WhenUpdateFinalizationTimestampWithEmptyPetugas_ThenThrows()
    {
        var bill = CreateBillWithTransComponents("BILL-04", ("KOMP-A", 100_000m, 0));
        bill.FinalizeAllocation(PaymentType.ByKas, 100_000m, "petugas-awal", "RO-001", DateTime.Now);

        var act = () => bill.UpdateFinalizationTimestamp(DateTime.Now, "  ");

        act.Should().Throw<ArgumentException>().WithMessage("*Petugas verif should not be empty*");
    }
}


