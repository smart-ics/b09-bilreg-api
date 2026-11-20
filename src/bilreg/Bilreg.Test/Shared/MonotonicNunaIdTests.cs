using System.Collections.Concurrent;
using System.Diagnostics;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.Shared;

public class MonotonicNunaIdTests
{
    private const int LoopCount = 1500;

    [Theory]
    [InlineData("SBD")]
    [InlineData("STM")]
    [InlineData("STL")]
    [InlineData("STB")]
    [InlineData("TBG")]
    [InlineData("PND")]
    public void New_ConsecutiveCallsInTightLoop_AreUniqueLength12AndStrictlyIncreasingOrdinal(string prefix)
    {
        var ids = new List<string>(LoopCount);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < LoopCount; i++)
            ids.Add(MonotonicNunaId.New(prefix));

        sw.Stop();

        ids.Should().HaveCount(LoopCount);
        ids.Distinct(StringComparer.Ordinal).Should().HaveCount(LoopCount);
        ids.Should().OnlyContain(id => id.Length == 12 && id.StartsWith(prefix, StringComparison.Ordinal));

        for (var i = 1; i < ids.Count; i++)
        {
            StringComparer.Ordinal.Compare(ids[i - 1], ids[i]).Should().BeLessThan(0,
                $"ID at index {i} ({ids[i]}) must be strictly greater than ({ids[i - 1]})");
        }

        // 1500 calls with Thread.Sleep(5) would take >= 7500ms; lock-based Base36 increment must finish well under 2000ms.
        sw.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Theory]
    [InlineData("BK")]
    [InlineData("ST")]
    [InlineData("PK")]
    public void NewLegacyCompact_ConsecutiveCallsInTightLoop_AreUniqueLength10AndStrictlyIncreasingOrdinal(string prefix)
    {
        var ids = new List<string>(LoopCount);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < LoopCount; i++)
            ids.Add(MonotonicNunaId.NewLegacyCompact(prefix));

        sw.Stop();

        ids.Should().HaveCount(LoopCount);
        ids.Distinct(StringComparer.Ordinal).Should().HaveCount(LoopCount);
        ids.Should().OnlyContain(id => id.Length == 10 && id.StartsWith(prefix, StringComparison.Ordinal));

        for (var i = 1; i < ids.Count; i++)
        {
            StringComparer.Ordinal.Compare(ids[i - 1], ids[i]).Should().BeLessThan(0,
                $"ID at index {i} ({ids[i]}) must be strictly greater than ({ids[i - 1]})");
        }

        sw.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public void New_WithMinExclusiveId_ExceedsMinExclusiveIdInSingleCall()
    {
        var baseline = MonotonicNunaId.New("STB");
        var minExclusive = BumpBase36Suffix(baseline, prefixLength: 3, delta: 500);

        var next = MonotonicNunaId.New("STB", minExclusive);

        next.Length.Should().Be(12);
        next.Should().StartWith("STB");
        StringComparer.Ordinal.Compare(next, minExclusive).Should().BeGreaterThan(0);
    }

    [Fact]
    public void NewLegacyCompact_WithMinExclusiveId_ExceedsMinExclusiveIdInSingleCall()
    {
        var baseline = MonotonicNunaId.NewLegacyCompact("BK");
        var minExclusive = BumpBase36Suffix(baseline, prefixLength: 2, delta: 500);

        var next = MonotonicNunaId.NewLegacyCompact("BK", minExclusive);

        next.Length.Should().Be(10);
        next.Should().StartWith("BK");
        StringComparer.Ordinal.Compare(next, minExclusive).Should().BeGreaterThan(0);
    }

    [Fact]
    public void New_And_NewLegacyCompact_ParallelCalls_AreThreadSafeAndUnique()
    {
        const int threadCount = 8;
        const int perThread = 500;
        var stdBag = new ConcurrentBag<string>();
        var compactBag = new ConcurrentBag<string>();

        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < perThread; i++)
            {
                stdBag.Add(MonotonicNunaId.New("STM"));
                compactBag.Add(MonotonicNunaId.NewLegacyCompact("BK"));
            }
        });

        stdBag.Should().HaveCount(threadCount * perThread);
        stdBag.Distinct(StringComparer.Ordinal).Should().HaveCount(threadCount * perThread);

        compactBag.Should().HaveCount(threadCount * perThread);
        compactBag.Distinct(StringComparer.Ordinal).Should().HaveCount(threadCount * perThread);
    }

    [Fact]
    public void DomainFactories_ConsecutiveCreationInTightLoop_ProduceUniqueMonotonicIds()
    {
        var tgl = new DateTime(2026, 4, 1, 10, 0, 0);
        var tglEd = new DateTime(2027, 6, 30);
        var layanan = new LayananReff("L01", "Poli");
        var item = PakaiBrgItemModel.Create(
            1, new BrgReff("B001", "Spuit"), new SatuanReff("PCS", "Pieces"),
            10, 1, "BATCH1", tglEd, 1000m, "");

        var batchIds = new List<string>(1000);
        var lokasiIds = new List<string>(1000);
        var mutasiInIds = new List<string>(1000);
        var mutasiOutIds = new List<string>(1000);
        var bindingMutasiIds = new List<string>(1000);
        var bindingLokasiIds = new List<string>(1000);
        var pakaiIds = new List<string>(1000);

        for (var i = 0; i < 1000; i++)
        {
            batchIds.Add(StockBatchModel.Create("BRG01", "DO01", 100m, tgl).StokBatchId);
            lokasiIds.Add(LocationStockBalanceModel.Create("STB01", "BRG01", "DO01", "L01", tglEd, tgl, 5m).StokLokasiId);
            mutasiInIds.Add(StockMovementModel.CreateInbound("STL01", "STB01", "BRG01", "DO01", "L01", tglEd, "TR01", MovementKindEnum.GoodsReceipt, 1m, 100m, tgl).StokMutasiId);
            mutasiOutIds.Add(StockMovementModel.CreateOutbound("STL01", "STB01", "BRG01", "DO01", "L01", tglEd, "TR02", MovementKindEnum.SaleIssueDb, 1m, 100m, tgl).StokMutasiId);
            bindingMutasiIds.Add(StockLegacyBindingModel.CreateMutasiBuku("STM01", "BK01", "TR01").BindingId);
            bindingLokasiIds.Add(StockLegacyBindingModel.CreateLokasiStok("STL01", "ST01", "TR01").BindingId);
            pakaiIds.Add(PakaiBrgModel.Create(tgl, layanan, "petugas01", "Note", [item]).PakaiBrgId);
        }

        AssertUniqueAndMonotonic(batchIds, "STB", 12);
        AssertUniqueAndMonotonic(lokasiIds, "STL", 12);
        AssertUniqueAndMonotonic(mutasiInIds, "STM", 12);
        AssertUniqueAndMonotonic(mutasiOutIds, "STM", 12);
        AssertUniqueAndMonotonic(bindingMutasiIds, "SBD", 12);
        AssertUniqueAndMonotonic(bindingLokasiIds, "SBD", 12);
        AssertUniqueAndMonotonic(pakaiIds, "PK", 10);
    }

    private static void AssertUniqueAndMonotonic(List<string> ids, string prefix, int expectedLength)
    {
        ids.Distinct(StringComparer.Ordinal).Should().HaveCount(ids.Count);
        ids.Should().OnlyContain(id => id.Length == expectedLength && id.StartsWith(prefix, StringComparison.Ordinal));
        for (var i = 1; i < ids.Count; i++)
            StringComparer.Ordinal.Compare(ids[i - 1], ids[i]).Should().BeLessThan(0);
    }

    private static string BumpBase36Suffix(string id, int prefixLength, long delta)
    {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var suffix = id[prefixLength..];
        long val = 0;
        foreach (var c in suffix)
            val = val * 36 + alphabet.IndexOf(char.ToUpperInvariant(c));

        val += delta;
        var chars = new char[suffix.Length];
        for (var i = suffix.Length - 1; i >= 0; i--)
        {
            chars[i] = alphabet[(int)(val % 36)];
            val /= 36;
        }

        return id[..prefixLength] + new string(chars);
    }
}
