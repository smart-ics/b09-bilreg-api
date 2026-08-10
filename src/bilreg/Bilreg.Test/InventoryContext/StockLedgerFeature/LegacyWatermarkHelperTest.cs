using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class LegacyWatermarkHelperTest
{
    private const string BrgId = "BRG0000000001";
    private const string DoId = "DO00000001";
    private static readonly DateTime RealWatermark = new(2026, 3, 1, 10, 0, 0);

    private static LegacyStockJournalReadModel Journal(string bukuId, DateTime tgl) =>
        new(bukuId, BrgId, DoId, "LY001", "", "",
            QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
            TrsReffId: "DM1", TglMutasi: tgl, TglEd: StockLedgerSentinel.EmptyDate);

    [Fact]
    public void IsAfterWatermark_EmptyDateAndEmptyBukuId_TreatsAnyJournalAsAfter()
    {
        var journal = Journal("BK00000001", new DateTime(2026, 1, 15, 8, 0, 0));

        LegacyWatermarkHelper.IsAfterWatermark(
                journal,
                StockLedgerSentinel.EmptyDate,
                lastLegacyBukuId: string.Empty)
            .Should().BeTrue();
    }

    [Fact]
    public void FilterAfterWatermark_EmptyWatermark_ReturnsAllJournals()
    {
        var journals = new[]
        {
            Journal("BK00000001", new DateTime(2026, 1, 15, 8, 0, 0)),
            Journal("BK00000002", new DateTime(2025, 6, 1, 0, 0, 0)),
        };

        var pending = LegacyWatermarkHelper.FilterAfterWatermark(
            journals,
            StockLedgerSentinel.EmptyDate,
            lastLegacyBukuId: string.Empty);

        pending.Should().HaveCount(2);
    }

    [Fact]
    public void IsAfterWatermark_RealWatermark_ExcludesOlderAndEqualOrLowerBukuId()
    {
        var older = Journal("BK00000000", RealWatermark.AddHours(-1));
        var atWatermarkSameBuku = Journal("BK00000001", RealWatermark);
        var atWatermarkHigherBuku = Journal("BK00000002", RealWatermark);
        var newer = Journal("BK00000003", RealWatermark.AddHours(1));

        LegacyWatermarkHelper.IsAfterWatermark(older, RealWatermark, "BK00000001")
            .Should().BeFalse();
        LegacyWatermarkHelper.IsAfterWatermark(atWatermarkSameBuku, RealWatermark, "BK00000001")
            .Should().BeFalse();
        LegacyWatermarkHelper.IsAfterWatermark(atWatermarkHigherBuku, RealWatermark, "BK00000001")
            .Should().BeTrue();
        LegacyWatermarkHelper.IsAfterWatermark(newer, RealWatermark, "BK00000001")
            .Should().BeTrue();
    }

    [Fact]
    public void HasLegacyRowsBeyondWatermark_EmptyAlignedScope_DetectsLaterJournals()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkAligned(StockLedgerSentinel.EmptyDate, string.Empty, DateTime.Now);

        var journals = new[]
        {
            Journal("BK00000001", new DateTime(2026, 4, 1, 12, 0, 0)),
        };

        LegacyWatermarkHelper.HasLegacyRowsBeyondWatermark(journals, scope)
            .Should().BeTrue();
    }
}
