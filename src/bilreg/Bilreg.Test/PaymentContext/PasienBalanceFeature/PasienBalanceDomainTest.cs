using Bilreg.Domain.PaymentContext.PasienBalanceFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.PasienBalanceFeature;

public class PasienBalanceDomainTest
{
    private const string PasienId = "112233400000821";
    private static readonly DateTime TrsDate1 = new(2026, 6, 29, 10, 0, 0);
    private static readonly DateTime TrsDate2 = new(2026, 6, 30, 9, 0, 0);

    [Fact]
    public void UT01_GivenNewPasien_WhenCreate_ThenShouldHaveZeroTotalsAndNoEntries()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        balance.PasienId.Should().Be(PasienId);
        balance.TotalOutstandingJasa.Should().Be(0m);
        balance.TotalOutstandingObat.Should().Be(0m);
        balance.TotalOutstanding.Should().Be(0m);
        balance.OutstandingEntries.Should().BeEmpty();
        balance.Version.Should().Be(0);
    }

    [Fact]
    public void UT02_GivenEmptyAggregate_WhenAddOutstanding_ThenShouldIncreaseTotals()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        balance.AddOutstanding("RG-001", 300_000m, 200_000m, TrsDate1, "PIU001", "kasir-01");

        balance.TotalOutstandingJasa.Should().Be(300_000m);
        balance.TotalOutstandingObat.Should().Be(200_000m);
        balance.TotalOutstanding.Should().Be(500_000m);
        balance.OutstandingEntries.Should().HaveCount(1);

        var entry = balance.OutstandingEntries.Single();
        entry.RegId.Should().Be("RG-001");
        entry.OutstandingJasa.Should().Be(300_000m);
        entry.OutstandingObat.Should().Be(200_000m);
        entry.OutstandingTotal.Should().Be(500_000m);
        entry.SourceReference.Should().Be("PIU001");
        entry.IsPersisted.Should().BeFalse();
    }

    [Fact]
    public void UT03_GivenExistingEntry_WhenAddDuplicateRegId_ThenShouldThrow()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        balance.AddOutstanding("RG-001", 100_000m, 50_000m, TrsDate1, "PIU001", "kasir-01");

        Action act = () => balance.AddOutstanding("RG-001", 10_000m, 0m, TrsDate2, "PIU002", "kasir-01");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RG-001*");
        balance.OutstandingEntries.Should().HaveCount(1);
    }

    [Fact]
    public void UT04_GivenExistingEntry_WhenUpdateOutstanding_ThenShouldReplaceAmounts()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        balance.AddOutstanding("RG-001", 100_000m, 50_000m, TrsDate1, "PIU001", "kasir-01");

        balance.UpdateOutstanding("RG-001", 80_000m, 40_000m, TrsDate2, "PIU001");

        balance.TotalOutstanding.Should().Be(120_000m);
        balance.OutstandingEntries.Single().OutstandingJasa.Should().Be(80_000m);
        balance.OutstandingEntries.Single().LastTransactionDate.Should().Be(TrsDate2);
    }

    [Fact]
    public void UT05_GivenExistingEntry_WhenRemoveOutstanding_ThenShouldClearEntryAndTotals()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        balance.AddOutstanding("RG-001", 100_000m, 50_000m, TrsDate1, "PIU001", "kasir-01");

        balance.RemoveOutstanding("RG-001");

        balance.OutstandingEntries.Should().BeEmpty();
        balance.TotalOutstanding.Should().Be(0m);
    }

    [Fact]
    public void UT06_GivenMissingEntry_WhenUpdateOrRemove_ThenShouldThrow()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        Action updateAct = () => balance.UpdateOutstanding("RG-999", 1m, 1m, TrsDate1, "");
        Action removeAct = () => balance.RemoveOutstanding("RG-999");

        updateAct.Should().Throw<InvalidOperationException>();
        removeAct.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UT07_GivenNegativeAmount_WhenAddOutstanding_ThenShouldThrow()
    {
        var balance = PasienBalanceModel.Create(PasienId);

        Action act = () => balance.AddOutstanding("RG-001", -1m, 0m, TrsDate1, "", "kasir-01");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UT08_GivenMultipleLegacyRows_WhenReplaceOutstandingEntries_ThenShouldPopulateCollection()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        var entries = new[]
        {
            new OutstandingEntryType("", PasienId, "RG-007", 300_000m, 200_000m, TrsDate1, "PIU007", DateTime.MinValue, "", false),
            new OutstandingEntryType("", PasienId, "RG-008", 150_000m, 100_000m, TrsDate2, "PIU008", DateTime.MinValue, "", false)
        };

        balance.ReplaceOutstandingEntries(entries, "bootstrap");

        balance.OutstandingEntries.Should().HaveCount(2);
        balance.TotalOutstandingJasa.Should().Be(450_000m);
        balance.TotalOutstandingObat.Should().Be(300_000m);
        balance.TotalOutstanding.Should().Be(750_000m);
        balance.OutstandingEntries.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.EntryId));
        balance.OutstandingEntries.Should().OnlyContain(x => x.CreatedBy == "bootstrap");
    }

    [Fact]
    public void UT09_GivenDuplicateRegIdInReplace_WhenReplaceOutstandingEntries_ThenShouldThrow()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        var entries = new[]
        {
            new OutstandingEntryType("", PasienId, "RG-007", 100m, 0m, TrsDate1, "", DateTime.MinValue, "", false),
            new OutstandingEntryType("", PasienId, "RG-007", 200m, 0m, TrsDate2, "", DateTime.MinValue, "", false)
        };

        Action act = () => balance.ReplaceOutstandingEntries(entries, "bootstrap");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplikat*");
    }

    [Fact]
    public void UT10_GivenMutations_WhenCompleted_ThenTotalInvariantHolds()
    {
        var balance = PasienBalanceModel.Create(PasienId);
        balance.AddOutstanding("RG-007", 300_000m, 200_000m, TrsDate1, "PIU007", "kasir-01");
        balance.AddOutstanding("RG-008", 150_000m, 100_000m, TrsDate2, "PIU008", "kasir-02");
        balance.UpdateOutstanding("RG-007", 250_000m, 150_000m, TrsDate2, "PIU007");
        balance.RemoveOutstanding("RG-008");

        balance.TotalOutstanding.Should().Be(400_000m);
        balance.TotalOutstanding.Should().Be(
            balance.OutstandingEntries.Sum(x => x.OutstandingTotal));
    }
}
