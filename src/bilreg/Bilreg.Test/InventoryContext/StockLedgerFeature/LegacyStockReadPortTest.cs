using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StokFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S1 / G-05 — Live read-only legacy reconstruction adapter against tb_stok / tb_buku.
/// </summary>
public class LegacyStockReadPortTest
{
    private readonly LegacyStockReadPort _sut = new(ConnStringHelper.GetTestEnv());
    private readonly tb_stok_dal _stokDal = new(ConnStringHelper.GetTestEnv());
    private readonly tb_buku_dal _bukuDal = new(ConnStringHelper.GetTestEnv());

    private static readonly IStockLedgerScopeKey Scope =
        StockLedgerScopeKeyType.Create("BRGS701", "DO7001");

    private static readonly IStockLedgerScopeKey EmptyScope =
        StockLedgerScopeKeyType.Create("BRGS701X", "DO7001X");

    [Fact]
    public void ListCurrentBalances_MultiLocation_ReturnsAllLocations()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedFixture();

        var balances = _sut.ListCurrentBalances(Scope);

        balances.Should().HaveCount(2);
        balances.Select(x => x.LayananId).Should().BeEquivalentTo(["GD70A", "GD70B"]);
        balances.Should().OnlyContain(x => x.BrgId == "BRGS701" && x.ReceiptSourceId == "DO7001");
        balances.Should().Contain(x => x.LegacyRowId == "ST701A" && x.Quantity == 10m);
        balances.Should().Contain(x => x.LegacyRowId == "ST701B" && x.Quantity == 5m);
    }

    [Fact]
    public void ListJournalEntries_MultiLocation_ReturnsAllLocationsIncludingHistory()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedFixture();

        var journals = _sut.ListJournalEntries(Scope);

        journals.Should().HaveCount(3);
        journals.Select(x => x.LayananId).Should().BeEquivalentTo(["GD70A", "GD70A", "GD70B"]);
        journals.Should().OnlyContain(x => x.BrgId == "BRGS701" && x.ReceiptSourceId == "DO7001");
        journals.Should().Contain(x => x.LegacyJournalId == "BK701A1" && x.QuantityIn == 10m);
        journals.Should().Contain(x => x.LegacyJournalId == "BK701A2" && x.QuantityOut == 2m);
        journals.Should().Contain(x => x.LegacyJournalId == "BK701B1" && x.QuantityIn == 5m);
    }

    [Fact]
    public void EmptyScope_ReturnsEmptyCollections()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedFixture();

        _sut.ListCurrentBalances(EmptyScope).Should().BeEmpty();
        _sut.ListJournalEntries(EmptyScope).Should().BeEmpty();
    }

    [Fact]
    public void Ordering_IsStable_AcrossRepeatedReads()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedFixture();

        var balances1 = _sut.ListCurrentBalances(Scope);
        var balances2 = _sut.ListCurrentBalances(Scope);
        var journals1 = _sut.ListJournalEntries(Scope);
        var journals2 = _sut.ListJournalEntries(Scope);

        balances1.Select(x => x.LegacyRowId).Should().Equal(balances2.Select(x => x.LegacyRowId));
        balances1.Select(x => x.LayananId).Should().Equal(["GD70A", "GD70B"]);

        journals1.Select(x => x.LegacyJournalId).Should().Equal(journals2.Select(x => x.LegacyJournalId));
        journals1.Select(x => x.LegacyJournalId).Should().Equal(["BK701A1", "BK701A2", "BK701B1"]);
    }

    [Fact]
    public void Reads_HaveNoWriteSideEffects()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        SeedFixture();

        var balancesBefore = _sut.ListCurrentBalances(Scope).ToList();
        var journalsBefore = _sut.ListJournalEntries(Scope).ToList();

        _ = _sut.ListCurrentBalances(Scope);
        _ = _sut.ListJournalEntries(Scope);

        var balancesAfter = _sut.ListCurrentBalances(Scope).ToList();
        var journalsAfter = _sut.ListJournalEntries(Scope).ToList();

        balancesAfter.Should().BeEquivalentTo(balancesBefore, opt => opt.WithStrictOrdering());
        journalsAfter.Should().BeEquivalentTo(journalsBefore, opt => opt.WithStrictOrdering());
    }

    private void SeedFixture()
    {
        _stokDal.Insert(StokRow(
            fs_kd_trs: "ST701A",
            fs_kd_layanan: "GD70A",
            fn_qty: 10m,
            fd_tgl_do: "2026-01-10",
            fs_jam_do: "08:00:00",
            fd_tgl_mutasi: "2026-01-10",
            fs_jam_mutasi: "08:00:00"));

        _stokDal.Insert(StokRow(
            fs_kd_trs: "ST701B",
            fs_kd_layanan: "GD70B",
            fn_qty: 5m,
            fd_tgl_do: "2026-01-11",
            fs_jam_do: "09:00:00",
            fd_tgl_mutasi: "2026-01-11",
            fs_jam_mutasi: "09:00:00"));

        _bukuDal.Insert([
            BukuRow(
                fs_kd_trs: "BK701A1",
                fs_kd_layanan: "GD70A",
                fn_stok_in: 10m,
                fn_stok_out: 0m,
                fs_kd_jenis_mutasi: "DO",
                fs_kd_mutasi: "DO7001",
                fd_tgl_jam_mutasi: "2026-01-10 08:00:00",
                fd_tgl_mutasi: "2026-01-10",
                fs_jam_mutasi: "08:00:00"),
            BukuRow(
                fs_kd_trs: "BK701A2",
                fs_kd_layanan: "GD70A",
                fn_stok_in: 0m,
                fn_stok_out: 2m,
                fs_kd_jenis_mutasi: "DU",
                fs_kd_mutasi: "FO701A",
                fd_tgl_jam_mutasi: "2026-01-10 10:00:00",
                fd_tgl_mutasi: "2026-01-10",
                fs_jam_mutasi: "10:00:00"),
            BukuRow(
                fs_kd_trs: "BK701B1",
                fs_kd_layanan: "GD70B",
                fn_stok_in: 5m,
                fn_stok_out: 0m,
                fs_kd_jenis_mutasi: "DO",
                fs_kd_mutasi: "DO7001",
                fd_tgl_jam_mutasi: "2026-01-11 09:00:00",
                fd_tgl_mutasi: "2026-01-11",
                fs_jam_mutasi: "09:00:00")
        ]);
    }

    private static tb_stok_dto StokRow(
        string fs_kd_trs,
        string fs_kd_layanan,
        decimal fn_qty,
        string fd_tgl_do,
        string fs_jam_do,
        string fd_tgl_mutasi,
        string fs_jam_mutasi)
        => new(
            fs_kd_trs: fs_kd_trs,
            fs_kd_barang: "BRGS701",
            fs_kd_layanan: fs_kd_layanan,
            fs_kd_po: "PO701",
            fs_kd_do: "DO7001",
            fd_tgl_ed: "2027-01-01",
            fs_no_batch: "BATCH701",
            fn_qty_in: fn_qty,
            fn_qty: fn_qty,
            fn_hpp: 1000m,
            fd_tgl_do: fd_tgl_do,
            fs_jam_do: fs_jam_do,
            fs_kd_mutasi: "DO7001",
            fd_tgl_mutasi: fd_tgl_mutasi,
            fs_jam_mutasi: fs_jam_mutasi,
            fs_kd_satuan: "TAB",
            fs_nm_barang: string.Empty,
            fs_nm_layanan: string.Empty);

    private static tb_buku_dto BukuRow(
        string fs_kd_trs,
        string fs_kd_layanan,
        decimal fn_stok_in,
        decimal fn_stok_out,
        string fs_kd_jenis_mutasi,
        string fs_kd_mutasi,
        string fd_tgl_jam_mutasi,
        string fd_tgl_mutasi,
        string fs_jam_mutasi)
        => new(
            fs_kd_trs: fs_kd_trs,
            fs_kd_barang: "BRGS701",
            fs_kd_layanan: fs_kd_layanan,
            fs_kd_po: "PO701",
            fs_kd_do: "DO7001",
            fd_tgl_ed: "2027-01-01",
            fs_no_batch: "BATCH701",
            fn_stok_in: fn_stok_in,
            fn_stok_out: fn_stok_out,
            fn_hpp: 1000m,
            fs_kd_mutasi: fs_kd_mutasi,
            fd_tgl_mutasi: fd_tgl_mutasi,
            fs_jam_mutasi: fs_jam_mutasi,
            fd_tgl_jam_mutasi: fd_tgl_jam_mutasi,
            fs_kd_jenis_mutasi: fs_kd_jenis_mutasi,
            fs_kd_satuan: "TAB",
            fs_nm_barang: string.Empty,
            fs_nm_layanan: string.Empty);
}
