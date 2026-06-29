using Bilreg.Domain.PaymentContext.PasienBalanceFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.PasienBalanceFeature;

public class PasienBalanceDomainTest
{
    private const string PasienId = "112233400000821";
    private static readonly DateTime TrsDate1 = new(2026, 6, 29, 10, 0, 0);
    private static readonly DateTime TrsDate2 = new(2026, 6, 29, 11, 0, 0);
    private static readonly DateTime TrsDate3 = new(2026, 6, 30, 9, 0, 0);
    private static readonly DateTime TrsDate4 = new(2026, 6, 30, 14, 0, 0);

    [Fact]
    public void UT01_GivenNewPasien_WhenCreate_ThenShouldHaveZeroPartitionAndTotalBalance()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        balance.PasienId.Should().Be(PasienId);
        balance.CurrentJasaBalance.Should().Be(0m);
        balance.CurrentObatBalance.Should().Be(0m);
        balance.CurrentBalance.Should().Be(0m);
        balance.ListHistory.Should().BeEmpty();
        balance.LastHistoryId.Should().Be("-");
        balance.Version.Should().Be(0);
    }

    [Fact]
    public void UT02_GivenZeroBalance_WhenApplyChargeMixed_ThenShouldIncreasePartitionsAndAppendHistory()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        balance.ApplyCharge(300_000m, 200_000m, "RG-001", TrsDate1, "Biaya registrasi", "kasir-01");

        balance.CurrentJasaBalance.Should().Be(300_000m);
        balance.CurrentObatBalance.Should().Be(200_000m);
        balance.CurrentBalance.Should().Be(500_000m);
        balance.ListHistory.Should().HaveCount(1);

        var history = balance.ListHistory.Single();
        history.OpeningJasaBalance.Should().Be(0m);
        history.OpeningObatBalance.Should().Be(0m);
        history.ChargeJasa.Should().Be(300_000m);
        history.ChargeObat.Should().Be(200_000m);
        history.PaymentJasa.Should().Be(0m);
        history.PaymentObat.Should().Be(0m);
        history.ClosingJasaBalance.Should().Be(300_000m);
        history.ClosingObatBalance.Should().Be(200_000m);
        history.ClosingBalance.Should().Be(500_000m);
        history.IsPersisted.Should().BeFalse();
    }

    [Fact]
    public void UT02b_GivenZeroBalance_WhenApplyChargeJasaOnly_ThenShouldIncreaseJasaOnly()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        balance.ApplyCharge(100_000m, 0m, "RG-001", TrsDate1, "Jasa only", "kasir-01");

        balance.CurrentJasaBalance.Should().Be(100_000m);
        balance.CurrentObatBalance.Should().Be(0m);
        balance.CurrentBalance.Should().Be(100_000m);
    }

    [Fact]
    public void UT03_GivenPositiveBalance_WhenApplyPayment_ThenShouldDecreasePartitions()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        balance.ApplyCharge(200_000m, 100_000m, "RG-001", TrsDate1, "Charge", "kasir-01");

        balance.ApplyPayment(50_000m, 30_000m, "RG-001", TrsDate2, "Bayar sebagian", "kasir-01");

        balance.CurrentJasaBalance.Should().Be(150_000m);
        balance.CurrentObatBalance.Should().Be(70_000m);
        balance.CurrentBalance.Should().Be(220_000m);

        var payment = balance.ListHistory.Last();
        payment.OpeningJasaBalance.Should().Be(200_000m);
        payment.OpeningObatBalance.Should().Be(100_000m);
        payment.PaymentJasa.Should().Be(50_000m);
        payment.PaymentObat.Should().Be(30_000m);
        payment.ClosingBalance.Should().Be(220_000m);
    }

    [Fact]
    public void UT04_GivenPositiveBalance_WhenApplyPaymentExceedsJasa_ThenShouldThrow()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        balance.ApplyCharge(100_000m, 50_000m, "RG-001", TrsDate1, "Charge", "kasir-01");

        Action act = () => balance.ApplyPayment(150_000m, 0m, "RG-001", TrsDate2, "Overpay JASA", "kasir-01");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JASA*");
        balance.CurrentJasaBalance.Should().Be(100_000m);
        balance.ListHistory.Should().HaveCount(1);
    }

    [Fact]
    public void UT04b_GivenPositiveBalance_WhenApplyPaymentExceedsObat_ThenShouldThrow()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        balance.ApplyCharge(100_000m, 50_000m, "RG-001", TrsDate1, "Charge", "kasir-01");

        Action act = () => balance.ApplyPayment(0m, 80_000m, "RG-001", TrsDate2, "Overpay OBAT", "kasir-01");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OBAT*");
        balance.CurrentObatBalance.Should().Be(50_000m);
        balance.ListHistory.Should().HaveCount(1);
    }

    [Fact]
    public void UT05_GivenMultiRegistrationScenario_WhenObatOverPayment_ThenShouldReject()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        balance.ApplyCharge(300_000m, 200_000m, "RG-007", TrsDate1, "Charge RG-007", "kasir-01");
        balance.ApplyPayment(200_000m, 100_000m, "RG-007", TrsDate2, "Payment RG-007", "kasir-01");
        balance.ApplyCharge(150_000m, 100_000m, "RG-008", TrsDate3, "Charge RG-008", "kasir-02");

        Action act = () => balance.ApplyPayment(100_000m, 250_000m, "RG-008", TrsDate4, "Overpay OBAT", "kasir-02");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OBAT*");
        balance.CurrentObatBalance.Should().Be(200_000m);
        balance.ListHistory.Should().HaveCount(3);
    }

    [Fact]
    public void UT05b_GivenMultiRegistrationScenario_WhenValidPayments_ThenShouldReachExpectedTotal()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        balance.ApplyCharge(300_000m, 200_000m, "RG-007", TrsDate1, "Charge RG-007", "kasir-01");
        balance.ApplyPayment(200_000m, 100_000m, "RG-007", TrsDate2, "Payment RG-007", "kasir-01");

        balance.ApplyCharge(150_000m, 100_000m, "RG-008", TrsDate3, "Charge RG-008", "kasir-02");
        balance.ApplyPayment(100_000m, 150_000m, "RG-008", TrsDate4, "Payment RG-008", "kasir-02");

        balance.CurrentJasaBalance.Should().Be(150_000m);
        balance.CurrentObatBalance.Should().Be(50_000m);
        balance.CurrentBalance.Should().Be(200_000m);
        balance.ListHistory.Should().HaveCount(4);
        balance.ListHistory.Last().ClosingBalance.Should().Be(200_000m);
    }

    [Fact]
    public void UT06_GivenBothAmountsZero_WhenApplyChargeOrPayment_ThenShouldThrow()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        Action chargeAct = () => balance.ApplyCharge(0m, 0m, "RG-001", TrsDate1, "Invalid", "kasir-01");
        Action paymentAct = () => balance.ApplyPayment(0m, 0m, "RG-001", TrsDate1, "Invalid", "kasir-01");

        chargeAct.Should().Throw<ArgumentException>();
        paymentAct.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UT07_GivenMutations_WhenCompleted_ThenPartitionAndTotalInvariantsHold()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        balance.ApplyCharge(300_000m, 200_000m, "RG-007", TrsDate1, "Charge", "kasir-01");
        balance.ApplyPayment(200_000m, 100_000m, "RG-007", TrsDate2, "Payment", "kasir-01");
        balance.ApplyCharge(150_000m, 100_000m, "RG-008", TrsDate3, "Charge", "kasir-02");
        balance.ApplyPayment(100_000m, 150_000m, "RG-008", TrsDate4, "Payment", "kasir-02");

        var latest = balance.ListHistory.Last();
        balance.CurrentJasaBalance.Should().Be(latest.ClosingJasaBalance);
        balance.CurrentObatBalance.Should().Be(latest.ClosingObatBalance);
        balance.CurrentBalance.Should().Be(latest.ClosingBalance);
        balance.CurrentBalance.Should().Be(balance.CurrentJasaBalance + balance.CurrentObatBalance);
    }
}
