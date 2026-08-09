using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockOutboundAllocatorTest
{
    private const string BrgId = "BRG0000000001";
    private const string OtherBrg = "BRG0000000002";
    private const string LayananA = "LY001";
    private const string LayananB = "LY002";

    private static readonly DateTime EdEarly = new(2026, 6, 30);
    private static readonly DateTime EdLate = new(2027, 12, 31);
    private static readonly DateTime MasukOld = new(2025, 1, 10, 8, 0, 0);
    private static readonly DateTime MasukMid = new(2025, 3, 15, 8, 0, 0);
    private static readonly DateTime MasukNew = new(2025, 6, 20, 8, 0, 0);

    private static StockAllocationCandidateType Cand(
        string stokLokasiId,
        string stokBatchId,
        string brgMasukReffId,
        DateTime tglEd,
        DateTime tglMasuk,
        decimal qtySisa,
        string? brgId = null,
        string? layananId = null) =>
        new(
            brgId ?? BrgId,
            layananId ?? LayananA,
            stokLokasiId,
            stokBatchId,
            brgMasukReffId,
            tglEd,
            tglMasuk,
            qtySisa);

    [Fact]
    public void Allocate_Fefo_ConsumesEarliestEdFirst()
    {
        var candidates = new[]
        {
            Cand("L2", "B2", "DO02", EdLate, MasukOld, 10),
            Cand("L1", "B1", "DO01", EdEarly, MasukNew, 10),
        };

        var result = StockOutboundAllocator.Allocate(BrgId, LayananA, 10, candidates);

        result.IsSuccess.Should().BeTrue();
        result.Lines.Should().ContainSingle();
        result.Lines[0].StokLokasiId.Should().Be("L1");
        result.Lines[0].TglEd.Should().Be(EdEarly);
        result.Lines[0].QtyAllocated.Should().Be(10);
    }

    [Fact]
    public void Allocate_Fifo_WhenAllEdSentinel_OrdersByTglMasukThenDo()
    {
        var candidates = new[]
        {
            Cand("L3", "B3", "DO03", StockLedgerSentinel.EmptyDate, MasukNew, 5),
            Cand("L1", "B1", "DO01", StockLedgerSentinel.EmptyDate, MasukOld, 5),
            Cand("L2", "B2", "DO02", StockLedgerSentinel.EmptyDate, MasukMid, 5),
        };

        var result = StockOutboundAllocator.Allocate(BrgId, LayananA, 12, candidates);

        result.IsSuccess.Should().BeTrue();
        result.Lines.Should().HaveCount(3);
        result.Lines[0].StokLokasiId.Should().Be("L1");
        result.Lines[0].QtyAllocated.Should().Be(5);
        result.Lines[1].StokLokasiId.Should().Be("L2");
        result.Lines[1].QtyAllocated.Should().Be(5);
        result.Lines[2].StokLokasiId.Should().Be("L3");
        result.Lines[2].QtyAllocated.Should().Be(2);
    }

    [Fact]
    public void Allocate_Fifo_SameTglMasuk_TieBreakByBrgMasukReffId()
    {
        var candidates = new[]
        {
            Cand("L2", "B2", "DO02", StockLedgerSentinel.EmptyDate, MasukOld, 10),
            Cand("L1", "B1", "DO01", StockLedgerSentinel.EmptyDate, MasukOld, 10),
        };

        var result = StockOutboundAllocator.Allocate(BrgId, LayananA, 10, candidates);

        result.IsSuccess.Should().BeTrue();
        result.Lines.Should().ContainSingle();
        result.Lines[0].BrgMasukReffId.Should().Be("DO01");
    }

    [Fact]
    public void Allocate_ExplicitEd_IgnoresOtherEds()
    {
        var candidates = new[]
        {
            Cand("L1", "B1", "DO01", EdEarly, MasukOld, 100),
            Cand("L2", "B2", "DO02", EdLate, MasukMid, 5),
            Cand("L3", "B3", "DO03", EdLate, MasukNew, 5),
        };

        var result = StockOutboundAllocator.Allocate(
            BrgId, LayananA, 8, candidates, explicitTglEd: EdLate);

        result.IsSuccess.Should().BeTrue();
        result.Lines.Should().HaveCount(2);
        result.Lines.Should().OnlyContain(l => l.TglEd == EdLate);
        result.Lines[0].StokLokasiId.Should().Be("L2");
        result.Lines[0].QtyAllocated.Should().Be(5);
        result.Lines[1].StokLokasiId.Should().Be("L3");
        result.Lines[1].QtyAllocated.Should().Be(3);
        result.AllocatedQty.Should().Be(8);
    }

    [Fact]
    public void Allocate_SameEd_TieBreakByTglMasuk()
    {
        var candidates = new[]
        {
            Cand("L2", "B2", "DO02", EdEarly, MasukNew, 10),
            Cand("L1", "B1", "DO01", EdEarly, MasukOld, 10),
        };

        var result = StockOutboundAllocator.Allocate(BrgId, LayananA, 10, candidates);

        result.IsSuccess.Should().BeTrue();
        result.Lines.Should().ContainSingle();
        result.Lines[0].StokLokasiId.Should().Be("L1");
        result.Lines[0].TglMasuk.Should().Be(MasukOld);
    }

    [Fact]
    public void Allocate_MultiBalanceSplit_SpansBalances()
    {
        var candidates = new[]
        {
            Cand("L1", "B1", "DO01", EdEarly, MasukOld, 3),
            Cand("L2", "B2", "DO02", EdLate, MasukMid, 4),
            Cand("L3", "B3", "DO03", EdLate, MasukNew, 10),
        };

        var result = StockOutboundAllocator.Allocate(BrgId, LayananA, 10, candidates);

        result.IsSuccess.Should().BeTrue();
        result.Lines.Should().HaveCount(3);
        result.Lines[0].StokLokasiId.Should().Be("L1");
        result.Lines[0].QtyAllocated.Should().Be(3);
        result.Lines[1].StokLokasiId.Should().Be("L2");
        result.Lines[1].QtyAllocated.Should().Be(4);
        result.Lines[2].StokLokasiId.Should().Be("L3");
        result.Lines[2].QtyAllocated.Should().Be(3);
        result.AllocatedQty.Should().Be(10);
        result.ShortfallQty.Should().Be(0);
    }

    [Fact]
    public void Allocate_Insufficient_ReturnsPartialLinesAndShortfall()
    {
        var candidates = new[]
        {
            Cand("L1", "B1", "DO01", EdEarly, MasukOld, 4),
            Cand("L2", "B2", "DO02", EdLate, MasukMid, 3),
        };

        var result = StockOutboundAllocator.Allocate(BrgId, LayananA, 20, candidates);

        result.IsSuccess.Should().BeFalse();
        result.AllocatedQty.Should().Be(7);
        result.ShortfallQty.Should().Be(13);
        result.RequestedQty.Should().Be(20);
        result.Lines.Should().HaveCount(2);
        result.Lines.Sum(l => l.QtyAllocated).Should().Be(7);
        result.Lines.Should().OnlyContain(l => l.QtyAllocated > 0);
    }

    [Fact]
    public void Allocate_DepletedExcluded()
    {
        var candidates = new[]
        {
            Cand("L0", "B0", "DO00", EdEarly, MasukOld, 0),
            Cand("L1", "B1", "DO01", EdLate, MasukMid, 5),
        };

        var result = StockOutboundAllocator.Allocate(BrgId, LayananA, 5, candidates);

        result.IsSuccess.Should().BeTrue();
        result.Lines.Should().ContainSingle();
        result.Lines[0].StokLokasiId.Should().Be("L1");
    }

    [Fact]
    public void Allocate_WrongItemOrLocation_Excluded()
    {
        var candidates = new[]
        {
            Cand("L-wrong-brg", "B1", "DO01", EdEarly, MasukOld, 50, brgId: OtherBrg),
            Cand("L-wrong-lay", "B2", "DO02", EdEarly, MasukMid, 50, layananId: LayananB),
            Cand("L-ok", "B3", "DO03", EdLate, MasukNew, 5),
        };

        var result = StockOutboundAllocator.Allocate(BrgId, LayananA, 5, candidates);

        result.IsSuccess.Should().BeTrue();
        result.Lines.Should().ContainSingle();
        result.Lines[0].StokLokasiId.Should().Be("L-ok");
    }

    [Fact]
    public void Allocate_ExplicitEd_InsufficientWhenOnlyOtherEdsPresent()
    {
        var candidates = new[]
        {
            Cand("L1", "B1", "DO01", EdEarly, MasukOld, 100),
        };

        var result = StockOutboundAllocator.Allocate(
            BrgId, LayananA, 10, candidates, explicitTglEd: EdLate);

        result.IsSuccess.Should().BeFalse();
        result.AllocatedQty.Should().Be(0);
        result.ShortfallQty.Should().Be(10);
        result.Lines.Should().BeEmpty();
    }

    [Fact]
    public void Allocate_Fefo_SentinelEdSortsAfterRealEd()
    {
        var candidates = new[]
        {
            Cand("L-sent", "B1", "DO01", StockLedgerSentinel.EmptyDate, MasukOld, 10),
            Cand("L-ed", "B2", "DO02", EdEarly, MasukNew, 10),
        };

        var result = StockOutboundAllocator.Allocate(BrgId, LayananA, 10, candidates);

        result.IsSuccess.Should().BeTrue();
        result.Lines.Should().ContainSingle();
        result.Lines[0].StokLokasiId.Should().Be("L-ed");
    }

    [Fact]
    public void Allocate_RejectsNonPositiveRequestedQty()
    {
        var act = () => StockOutboundAllocator.Allocate(
            BrgId, LayananA, 0, Array.Empty<StockAllocationCandidateType>());

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromLokasi_MapsFieldsWithoutNoBatch()
    {
        var batch = StockBatchModel.Create(BrgId, "DO01", hpp: 100m, MasukOld);
        var lokasi = batch.IncreaseLokasi(LayananA, EdEarly, qty: 7, noBatch: "NB-IGNORE");

        var candidate = StockAllocationCandidateType.FromLokasi(lokasi);

        candidate.BrgId.Should().Be(BrgId);
        candidate.LayananId.Should().Be(LayananA);
        candidate.StokLokasiId.Should().Be(lokasi.StokLokasiId);
        candidate.StokBatchId.Should().Be(batch.StokBatchId);
        candidate.BrgMasukReffId.Should().Be("DO01");
        candidate.TglEd.Should().Be(EdEarly);
        candidate.TglMasuk.Should().Be(MasukOld);
        candidate.QtySisa.Should().Be(7);
    }
}
