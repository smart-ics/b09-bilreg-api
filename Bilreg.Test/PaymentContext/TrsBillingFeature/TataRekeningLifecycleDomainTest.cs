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

public class TataRekeningLifecycleDomainTest
{
    private const string REG_ID = "REG-LIFE-001";

    [Fact]
    public void UT01_GivenListTrsBill_WhenLifecycleOpenToLunas_ThenShouldReachLunas()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-JASA-01", BillModulGroup.Jasa, 100_000m));

        tataRekening.Close();
        tataRekening.Discharge(
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)],
            "kasir-01",
            new DateTime(2026, 6, 16, 10, 0, 0));

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);

        tataRekening.Pay(
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)],
            "PAY-001",
            new DateTime(2026, 6, 16, 11, 0, 0));

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Lunas);
    }

    [Fact]
    public void UT02_GivenClosedTataRekening_WhenCreateBill_ThenShouldThrow()
    {
        var tataRekening = TataRekeningModel.Create(REG_ID);
        tataRekening.Close();

        Action act = () => tataRekening.EnsureCanCreateTrsBill();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT03_GivenFinalizedTataRekening_WhenCreateBill_ThenShouldThrow()
    {
        var tataRekening = CreateFinalizedTataRekening();

        Action act = () => tataRekening.EnsureCanCreateTrsBill();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT04_GivenLunasTataRekening_WhenCreateBill_ThenShouldThrow()
    {
        var tataRekening = CreateLunasTataRekening();

        Action act = () => tataRekening.EnsureCanCreateTrsBill();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT05_GivenClosedTataRekening_WhenDeleteBill_ThenShouldThrow()
    {
        var bill = CreateBill("BILL-DEL-01", BillModulGroup.Jasa, 50_000m);
        var tataRekening = HydrateOpened(bill);
        tataRekening.Close();

        Action act = () => tataRekening.DeleteBill(bill.TrsBillingId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT06_GivenFinalizedTataRekening_WhenDeleteBill_ThenShouldThrow()
    {
        var tataRekening = CreateFinalizedTataRekening();

        Action act = () => tataRekening.DeleteBill("BILL-JASA-01");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT07_GivenOpenedTataRekening_WhenDeleteBill_ThenShouldRemoveFromListTrsBill()
    {
        var bill = CreateBill("BILL-DEL-02", BillModulGroup.Jasa, 50_000m);
        var tataRekening = HydrateOpened(bill);

        tataRekening.DeleteBill(bill.TrsBillingId);

        tataRekening.ListTrsBill.Should().BeEmpty();
    }

    [Fact]
    public void UT08_GivenPartialDischargeInput_WhenDischarge_ThenShouldReject()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-JASA-02", BillModulGroup.Jasa, 100_000m));
        tataRekening.Close();

        Action act = () => tataRekening.Discharge(
            [BuildPayment(PaymentType.ByKas, 50_000m, 0m)],
            "kasir-01",
            DateTime.Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak sama*");
    }

    [Fact]
    public void UT09_GivenNoPayment_WhenCancelDischarge_ThenShouldRevertToClosed()
    {
        var tataRekening = CreateFinalizedTataRekening();

        tataRekening.CancelDischarge();

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Closed);
        tataRekening.ListTrsBill.Single().ListDischarge.Should().BeEmpty();
    }

    [Fact]
    public void UT10_GivenPaymentExists_WhenCancelDischarge_ThenShouldReject()
    {
        var tataRekening = CreateFinalizedTataRekening();
        tataRekening.Pay(
            [BuildPayment(PaymentType.ByKas, 50_000m, 0m)],
            "PAY-002",
            DateTime.Now);

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);

        Action act = () => tataRekening.CancelDischarge();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah ada pembayaran*");
    }

    [Fact]
    public void UT11_GivenDischargedBill_WhenPay_ThenPaymentComponentsDeriveFromDischargeComponents()
    {
        var tataRekening = CreateFinalizedTataRekening();

        tataRekening.Pay(
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)],
            "PAY-003",
            new DateTime(2026, 6, 16, 12, 0, 0));

        var bill = tataRekening.ListTrsBill.Single();
        var discharge = bill.ListDischarge.Single();
        var payment = bill.ListPayment.Single();

        payment.Komponen.BillKompId.Should().Be(discharge.Komponen.BillKompId);
        payment.JenisBayar.Should().Be(discharge.JenisBayar);
        payment.Nilai.Should().Be(discharge.Nilai);
    }

    [Fact]
    public void UT12_GivenJasaAndObatBills_WhenDischarge_ThenEachModulGroupReceivesOnlyItsAllocation()
    {
        var jasaBill = CreateBill("BILL-JASA-03", BillModulGroup.Jasa, 80_000m);
        var obatBill = CreateBill("BILL-OBAT-01", BillModulGroup.Obat, 20_000m);
        var tataRekening = HydrateOpened(jasaBill, obatBill);
        tataRekening.Close();

        tataRekening.Discharge(
            [BuildPayment(PaymentType.ByKas, 80_000m, 20_000m)],
            "kasir-01",
            DateTime.Now);

        jasaBill.ListDischarge.Sum(x => x.Nilai).Should().Be(80_000m);
        obatBill.ListDischarge.Sum(x => x.Nilai).Should().Be(20_000m);
        jasaBill.ListDischarge.Should().NotBeEmpty();
        obatBill.ListDischarge.Should().NotBeEmpty();
    }

    [Fact]
    public void UT13_GivenJasaAllocationWithoutJasaBills_WhenDischarge_ThenShouldReject()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-OBAT-02", BillModulGroup.Obat, 20_000m));
        tataRekening.Close();

        Action act = () => tataRekening.Discharge(
            [BuildPayment(PaymentType.ByKas, 10_000m, 20_000m)],
            "kasir-01",
            DateTime.Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak sama*");
    }

    [Fact]
    public void UT14_GivenLunasTataRekening_WhenMutate_ThenShouldReject()
    {
        var tataRekening = CreateLunasTataRekening();

        Action close = () => tataRekening.Close();
        Action reopen = () => tataRekening.ReOpen();
        Action discharge = () => tataRekening.Discharge([], "kasir", DateTime.Now);
        Action pay = () => tataRekening.Pay([], "PAY", DateTime.Now);
        Action cancel = () => tataRekening.CancelDischarge();
        Action delete = () => tataRekening.DeleteBill("BILL-JASA-01");

        close.Should().Throw<InvalidOperationException>().WithMessage("*LUNAS*");
        reopen.Should().Throw<InvalidOperationException>().WithMessage("*LUNAS*");
        discharge.Should().Throw<InvalidOperationException>().WithMessage("*LUNAS*");
        pay.Should().Throw<InvalidOperationException>().WithMessage("*LUNAS*");
        cancel.Should().Throw<InvalidOperationException>().WithMessage("*LUNAS*");
        delete.Should().Throw<InvalidOperationException>().WithMessage("*LUNAS*");
    }

    private static TataRekeningModel HydrateOpened(params TrsBillType[] listTrsBill) =>
        new(REG_ID, TataRekeningStatusEnum.Opened, TataRekeningDischargeType.Default, [], listTrsBill);

    private static TataRekeningModel CreateFinalizedTataRekening()
    {
        var tataRekening = HydrateOpened(CreateBill("BILL-JASA-01", BillModulGroup.Jasa, 100_000m));
        tataRekening.Close();
        tataRekening.Discharge(
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)],
            "kasir-01",
            new DateTime(2026, 6, 16, 10, 0, 0));
        return tataRekening;
    }

    private static TataRekeningModel CreateLunasTataRekening()
    {
        var tataRekening = CreateFinalizedTataRekening();
        tataRekening.Pay(
            [BuildPayment(PaymentType.ByKas, 100_000m, 0m)],
            "PAY-LUNAS",
            new DateTime(2026, 6, 16, 11, 0, 0));
        return tataRekening;
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

    private static TrsBillType CreateBill(string billId, BillModulGroup modulGroup, decimal amount)
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
            modulGroup,
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
