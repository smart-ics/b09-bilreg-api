using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StokFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S7 / G-08 — Live provisional Availability Discovery against tb_stok.
/// Discovery only: no FIFO selection, no Freshness Gate, no legacy writes.
/// </summary>
public class AvailabilityDiscoveryPortTest
{
    private readonly AvailabilityDiscoveryPort _sut = new(ConnStringHelper.GetTestEnv());
    private readonly tb_stok_dal _stokDal = new(ConnStringHelper.GetTestEnv());
    private readonly StockPositionRepo _positionRepo = new(
        new StockPositionDal(ConnStringHelper.GetTestEnv()),
        new StockLayerDal(ConnStringHelper.GetTestEnv()));

    private static readonly IBrgKey Item = new BrgReff("BRGS807", "Avail Item");
    private static readonly ILayananKey Location = LayananType.Key("GD807");
    private static readonly DateOnly EdA = new(2027, 6, 1);

    [Fact]
    public void Discover_MultipleReceiptSources_ReturnsAllCandidates()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedMultiDoFixture();

        var result = _sut.Discover(Item, Location);

        result.Outcome.Should().Be(AvailabilityDiscoveryOutcomeEnum.CandidatesFound);
        result.Candidates.Should().HaveCount(3);
        result.Candidates.Select(x => x.ReceiptSourceId)
            .Should().BeEquivalentTo(["DO807A", "DO807B", "DO807C"]);
        result.Candidates.Should().OnlyContain(x => x.LayananId == "GD807");
        result.Candidates.Should().Contain(x => x.ReceiptSourceId == "DO807A" && x.AvailableQuantity == 10m);
        result.Candidates.Should().Contain(x => x.ReceiptSourceId == "DO807B" && x.AvailableQuantity == 7m);
        result.Candidates.Should().Contain(x => x.ReceiptSourceId == "DO807C" && x.AvailableQuantity == 4m);
        result.Candidates.Should().NotContain(x => x.ReceiptSourceId == "DO807X");
        result.Explanation.Should().Contain("not final FIFO");
    }

    [Fact]
    public void Discover_OptionalExpirationDateFilter_ReturnsMatchingCandidatesOnly()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedMultiDoFixture();

        var result = _sut.Discover(Item, Location, expirationDateFilter: EdA);

        result.Outcome.Should().Be(AvailabilityDiscoveryOutcomeEnum.CandidatesFound);
        result.Candidates.Should().HaveCount(2);
        result.Candidates.Should().OnlyContain(x => x.ExpirationDate == EdA);
        result.Candidates.Select(x => x.ReceiptSourceId)
            .Should().BeEquivalentTo(["DO807A", "DO807B"]);
        result.Candidates.Should().NotContain(x => x.ReceiptSourceId == "DO807C");
    }

    [Fact]
    public void Discover_EmptyOrNoMatchingStock_ReturnsInsufficientAuthoritativeStock()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedMultiDoFixture();

        var emptyItem = new BrgReff("BRGS807Z", "Missing");
        var emptyResult = _sut.Discover(emptyItem, Location);
        emptyResult.Outcome.Should().Be(AvailabilityDiscoveryOutcomeEnum.InsufficientAuthoritativeStock);
        emptyResult.Candidates.Should().BeEmpty();

        var edMiss = _sut.Discover(Item, Location, expirationDateFilter: new DateOnly(2099, 1, 1));
        edMiss.Outcome.Should().Be(AvailabilityDiscoveryOutcomeEnum.InsufficientAuthoritativeStock);
        edMiss.Candidates.Should().BeEmpty();

        var unusedLoc = _sut.Discover(Item, LayananType.Key("GD80Z"));
        unusedLoc.Outcome.Should().Be(AvailabilityDiscoveryOutcomeEnum.InsufficientAuthoritativeStock);
        unusedLoc.Candidates.Should().BeEmpty();
    }

    [Fact]
    public void Discover_DepletedOrZeroQuantityRows_AreNotOffered()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();

        _stokDal.Insert(StokRow(
            fs_kd_trs: "ST807Z0",
            fs_kd_do: "DO807Z",
            fn_qty: 0m,
            fd_tgl_ed: "2027-06-01",
            fs_no_batch: "BATCH-Z"));

        _stokDal.Insert(StokRow(
            fs_kd_trs: "ST807OK",
            fs_kd_do: "DO807OK",
            fn_qty: 3m,
            fd_tgl_ed: "2027-06-01",
            fs_no_batch: "BATCH-OK"));

        var result = _sut.Discover(Item, Location);

        result.Outcome.Should().Be(AvailabilityDiscoveryOutcomeEnum.CandidatesFound);
        result.Candidates.Should().ContainSingle();
        result.Candidates[0].ReceiptSourceId.Should().Be("DO807OK");
        result.Candidates[0].AvailableQuantity.Should().Be(3m);
        result.Candidates.Should().NotContain(x => x.ReceiptSourceId == "DO807Z");
    }

    [Fact]
    public void Discover_ReflectsLegacyAuthority_NotStockLedgerLayers()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();

        _stokDal.Insert(StokRow(
            fs_kd_trs: "ST807LG",
            fs_kd_do: "DO807LG",
            fn_qty: 5m,
            fd_tgl_ed: "2027-06-01",
            fs_no_batch: "BATCH-LG"));

        // Seed a reconstructed-looking Ledger position with a different remaining qty.
        // Discovery must still report legacy tb_stok quantity (5), not Ledger (999).
        var receiptSource = ReceiptSourceType.Create("DO807LG");
        var position = StockPositionModel.CreateEmpty(Item, receiptSource, Location)
            .AddLayer(StockLayerModel.Create(
                Item,
                receiptSource,
                Location,
                StockMovementModel.Key("MOVS807LG"),
                initialQuantity: 999m,
                UnitValuationType.Create(1000m),
                effectiveReceiptTime: new DateTime(2026, 1, 1, 8, 0, 0),
                origin: StockFactOriginEnum.Reconstructed,
                expirationDate: EdA,
                batch: "BATCH-LG",
                stockLayerId: "LYRS807LG"));
        _positionRepo.SaveChanges(position);

        var result = _sut.Discover(Item, Location);

        result.Outcome.Should().Be(AvailabilityDiscoveryOutcomeEnum.CandidatesFound);
        result.Candidates.Should().ContainSingle();
        result.Candidates[0].ReceiptSourceId.Should().Be("DO807LG");
        result.Candidates[0].AvailableQuantity.Should().Be(5m);
        result.Candidates[0].AvailableQuantity.Should().NotBe(999m);
    }

    [Fact]
    public void Discover_DoesNotPerformFifoSelection_ReturnsAllEligibleCandidates()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedMultiDoFixture();

        var result = _sut.Discover(Item, Location);

        // FIFO would pick a single oldest layer; discovery must return every eligible candidate.
        result.Candidates.Should().HaveCount(3);
        result.Candidates.Select(x => x.ReceiptSourceId).Distinct().Should().HaveCount(3);
        result.Outcome.Should().NotBe(AvailabilityDiscoveryOutcomeEnum.StaleOrNotCurrent);

        // Deterministic DO order is listing order, not an allocation decision.
        result.Candidates.Select(x => x.ReceiptSourceId)
            .Should().Equal("DO807A", "DO807B", "DO807C");
    }

    [Fact]
    public void Discover_HasNoWriteSideEffects()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedMultiDoFixture();

        var before = _sut.Discover(Item, Location);
        _ = _sut.Discover(Item, Location, expirationDateFilter: EdA);
        var after = _sut.Discover(Item, Location);

        after.Should().BeEquivalentTo(before);
        after.Candidates.Should().HaveCount(3);
    }

    private void SeedMultiDoFixture()
    {
        _stokDal.Insert(StokRow(
            fs_kd_trs: "ST807A",
            fs_kd_do: "DO807A",
            fn_qty: 10m,
            fd_tgl_ed: "2027-06-01",
            fs_no_batch: "BATCH-A"));

        _stokDal.Insert(StokRow(
            fs_kd_trs: "ST807B",
            fs_kd_do: "DO807B",
            fn_qty: 7m,
            fd_tgl_ed: "2027-06-01",
            fs_no_batch: "BATCH-B"));

        _stokDal.Insert(StokRow(
            fs_kd_trs: "ST807C",
            fs_kd_do: "DO807C",
            fn_qty: 4m,
            fd_tgl_ed: "2028-03-15",
            fs_no_batch: "BATCH-C"));

        // Different location — must not appear for GD807 discovery.
        _stokDal.Insert(StokRow(
            fs_kd_trs: "ST807X",
            fs_kd_do: "DO807X",
            fs_kd_layanan: "GD80X",
            fn_qty: 50m,
            fd_tgl_ed: "2027-06-01",
            fs_no_batch: "BATCH-X"));
    }

    private static tb_stok_dto StokRow(
        string fs_kd_trs,
        string fs_kd_do,
        decimal fn_qty,
        string fd_tgl_ed,
        string fs_no_batch,
        string fs_kd_layanan = "GD807")
        => new(
            fs_kd_trs: fs_kd_trs,
            fs_kd_barang: "BRGS807",
            fs_kd_layanan: fs_kd_layanan,
            fs_kd_po: "PO807",
            fs_kd_do: fs_kd_do,
            fd_tgl_ed: fd_tgl_ed,
            fs_no_batch: fs_no_batch,
            fn_qty_in: fn_qty,
            fn_qty: fn_qty,
            fn_hpp: 1000m,
            fd_tgl_do: "2026-01-10",
            fs_jam_do: "08:00:00",
            fs_kd_mutasi: fs_kd_do,
            fd_tgl_mutasi: "2026-01-10",
            fs_jam_mutasi: "08:00:00",
            fs_kd_satuan: "TAB",
            fs_nm_barang: string.Empty,
            fs_nm_layanan: string.Empty);
}
