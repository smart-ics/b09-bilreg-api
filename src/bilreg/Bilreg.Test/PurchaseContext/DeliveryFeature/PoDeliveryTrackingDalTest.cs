using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

[Collection(DeliveryDatabaseCollection.Name)]
public class PoDeliveryTrackingDalTest
{
    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly PoDeliveryTrackingDal _sut;

    public PoDeliveryTrackingDalTest()
    {
        _dbOptions = ConnStringHelper.GetTestEnv();
        _sut = new PoDeliveryTrackingDal(_dbOptions);
    }

    private static string NewTestPoId() => $"POT{Random.Shared.Next(1000000, 9999999)}";

    private void InsertTestPo(string poId, string permintaanId = "", decimal statusDo = 0, string tglTrs = "2026-10-15", string supplierId = "SUP01")
    {
        const string sql = """
            INSERT INTO tb_trs_po (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fs_kd_iii, fs_kd_permintaan, fs_keterangan, fs_keterangan2, fs_keterangan3, fs_no_spk,
                fn_sub_total, fn_tax_rupiah, fn_total, fn_diskon_lain, fn_biaya_lain, fn_grand_total,
                fn_status_do, fn_status_faktur, fs_kd_pabrik, fb_pembelian_bebas, fb_donasi, fb_closed,
                FS_NO_DOKUMEN, FD_TGL_APPROVE, FS_JAM_APPROVE, FS_KD_PTG_APPROVE, FD_TGL_CETAK, FS_JAM_CETAK,
                FS_KD_PTG_CETAK, FS_KD_GRUP_REK_DK, FS_KD_JENIS_BARANG, CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR, fd_tgl_closed, fs_jam_closed, fs_kd_petugas_closed,
                fd_tgl_open, fs_jam_open, fs_kd_petugas_open, fs_kd_jenis_beli
            ) VALUES (
                @PoId, @TglTrs, '10:00:00', 'TEST', '3000-01-01', '00:00:00', '',
                @SupplierId, @PermintaanId, 'Test PO', '', '', '',
                1000, 100, 1100, 0, 0, 1100,
                @StatusDo, 0, '', 0, 0, 0,
                '', '3000-01-01', '00:00:00', '', '3000-01-01', '00:00:00',
                '', '', '', @TglTrs, '10:00:00', '', '', 'TEST',
                '3000-01-01', '00:00:00', '', '', '', '3000-01-01', '00:00:00', '',
                '3000-01-01', '00:00:00', '', ''
            );
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(sql, new { PoId = poId, PermintaanId = permintaanId, StatusDo = statusDo, TglTrs = tglTrs, SupplierId = supplierId });
    }

    private void InsertTestPoItem(string poId, int noUrut, string brgId, decimal qty, decimal qtyDo, bool isClosed, bool isVoid = false)
    {
        const string sql = """
            INSERT INTO tb_trs_po2 (
                fs_kd_trs, fn_no_urut, fs_kd_barang, fn_harga, fn_diskon_prosen, fn_diskon,
                fn_qty, fn_qty_do, fn_qty_faktur, fn_qty_stok, fn_qty_stok_reff, fs_kd_satuan,
                fn_sub_total, fn_tax_prosen, fn_tax_rupiah, fn_biaya_lain, fn_total, fb_void,
                fn_nilai_akhir, fn_harga_last, fs_spek, fb_closed_po, fs_kd_nilai_reff, fn_biaya,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR, UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR,
                fn_konversi, fs_ket_detil
            ) VALUES (
                @PoId, @NoUrut, @BrgId, 1000, 0, 0,
                @Qty, @QtyDo, 0, 0, 0, 'BOK',
                @Qty * 1000, 11, (@Qty * 1000) * 0.11, 0, (@Qty * 1000) * 1.11, @IsVoid,
                0, 0, '', @IsClosed, '', 0,
                '2026-10-15', '10:00:00', '', '', 'TEST', '3000-01-01', '00:00:00', '', '', '',
                1, ''
            );
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(sql, new { PoId = poId, NoUrut = noUrut, BrgId = brgId, Qty = qty, QtyDo = qtyDo, IsClosed = isClosed, IsVoid = isVoid });
    }

    private static string NewTestDoId() => $"DMT{Random.Shared.Next(1000000, 9999999)}";

    private void InsertTestDo(string doId, string poId, string suratJalanNo, string tglVoid = "3000-01-01", string tglTrs = "2026-10-15")
    {
        const string sql = """
            INSERT INTO TB_TRS_DO (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fs_kd_po, fs_kd_tb, fs_kd_do_asli, fs_kd_layanan, fs_kd_iii, fs_kd_bap, fn_total,
                fs_kd_trs_hna, fb_pembelian_bebas, fs_keterangan, fn_genkeu, fs_kd_trs_fk, fn_status_bridge,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR, UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            ) VALUES (
                @DoId, @TglTrs, '10:00:00', 'TEST', @TglVoid, '00:00:00', '',
                @PoId, '', @SuratJalanNo, '4FM02', 'SUP01', '', 1000,
                '', 0, '', 0, '', 0,
                @TglTrs, '10:00:00', '', '', 'TEST', '3000-01-01', '00:00:00', '', '', ''
            );
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(sql, new { DoId = doId, PoId = poId, SuratJalanNo = suratJalanNo, TglVoid = tglVoid, TglTrs = tglTrs });
    }

    private void InsertTestDoInspection(string doId, int noUrut, string poId, string status = "Draft")
    {
        const string sql = """
            INSERT INTO BILRG_DoInspeksi (
                DoId, NoUrut, BrgId, PoId, PoNoUrut,
                QtyKirim, QtyDiterima, QtyDitolak, AlasanTolak,
                IsBonus, HasJaminanRetur, NoSuratJaminan, Status,
                ConfirmedAt, ConfirmedBy, VoidedAt, VoidedBy,
                CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            ) VALUES (
                @DoId, @NoUrut, '1.DUMI.2', @PoId, 1,
                10, 10, 0, '',
                0, 0, '', @Status,
                '3000-01-01', '', '3000-01-01', '',
                '2026-10-15', 'TEST', '3000-01-01', ''
            );
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(sql, new { DoId = doId, NoUrut = noUrut, PoId = poId, Status = status });
    }

    [Fact]
    public void GetRemainingQuota_WhenPoHasItems_ReturnsRemainingQuotaAndCalculatesSisaQty()
    {
        using var trans = TransHelper.NewScope();
        var poId = NewTestPoId();
        InsertTestPo(poId);
        InsertTestPoItem(poId, 1, "1.DUMI.2", qty: 10, qtyDo: 3, isClosed: false);
        InsertTestPoItem(poId, 2, "1.DUMI.2", qty: 5, qtyDo: 5, isClosed: true);

        var actual = _sut.GetRemainingQuota(poId).ToList();

        actual.Should().HaveCount(2);

        var item1 = actual.First(x => x.NoUrut == 1);
        item1.PoId.Should().Be(poId);
        item1.BrgId.Should().Be("1.DUMI.2");
        item1.SatuanKecilId.Should().Be("BOK");
        item1.QtyPo.Should().Be(10);
        item1.QtyDo.Should().Be(3);
        item1.SisaQty.Should().Be(7);
        item1.IsClosed.Should().BeFalse();

        var item2 = actual.First(x => x.NoUrut == 2);
        item2.QtyPo.Should().Be(5);
        item2.QtyDo.Should().Be(5);
        item2.SisaQty.Should().Be(0);
        item2.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void GetRemainingQuota_WhenPoDoesNotExist_ReturnsEmpty()
    {
        using var trans = TransHelper.NewScope();
        var actual = _sut.GetRemainingQuota("NONEXISTENT_PO");
        actual.Should().BeEmpty();
    }

    [Fact]
    public void IncrementReceivedQty_WhenPartialReceipt_UpdatesQtyDoAndKeepsOpen()
    {
        using var trans = TransHelper.NewScope();
        var poId = NewTestPoId();
        InsertTestPo(poId);
        InsertTestPoItem(poId, 1, "1.DUMI.2", qty: 10, qtyDo: 2, isClosed: false);

        _sut.IncrementReceivedQty(poId, 1, 3m);

        var actual = _sut.GetRemainingQuota(poId).First(x => x.NoUrut == 1);
        actual.QtyDo.Should().Be(5);
        actual.SisaQty.Should().Be(5);
        actual.IsClosed.Should().BeFalse();
    }

    [Fact]
    public void IncrementReceivedQty_WhenReceiptMeetsOrExceedsQty_UpdatesQtyDoAndSetsClosedPo()
    {
        using var trans = TransHelper.NewScope();
        var poId = NewTestPoId();
        InsertTestPo(poId);
        InsertTestPoItem(poId, 1, "1.DUMI.2", qty: 10, qtyDo: 8, isClosed: false);

        _sut.IncrementReceivedQty(poId, 1, 2m);

        var actual = _sut.GetRemainingQuota(poId).First(x => x.NoUrut == 1);
        actual.QtyDo.Should().Be(10);
        actual.SisaQty.Should().Be(0);
        actual.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void UpdatePoOverallStatus_WhenAllItemsClosed_SetsStatusDoTo2()
    {
        using var trans = TransHelper.NewScope();
        var poId = NewTestPoId();
        InsertTestPo(poId, statusDo: 0);
        InsertTestPoItem(poId, 1, "1.DUMI.2", qty: 10, qtyDo: 10, isClosed: true);
        InsertTestPoItem(poId, 2, "1.DUMI.2", qty: 5, qtyDo: 5, isClosed: true);

        _sut.UpdatePoOverallStatus(poId);

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        var status = conn.ExecuteScalar<decimal>("SELECT fn_status_do FROM tb_trs_po WHERE fs_kd_trs = @PoId", new { PoId = poId });
        status.Should().Be(2);
    }

    [Fact]
    public void UpdatePoOverallStatus_WhenSomeItemsReceivedOrClosed_SetsStatusDoTo1()
    {
        using var trans = TransHelper.NewScope();
        var poId = NewTestPoId();
        InsertTestPo(poId, statusDo: 0);
        InsertTestPoItem(poId, 1, "1.DUMI.2", qty: 10, qtyDo: 5, isClosed: false);
        InsertTestPoItem(poId, 2, "1.DUMI.2", qty: 5, qtyDo: 0, isClosed: false);

        _sut.UpdatePoOverallStatus(poId);

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        var status = conn.ExecuteScalar<decimal>("SELECT fn_status_do FROM tb_trs_po WHERE fs_kd_trs = @PoId", new { PoId = poId });
        status.Should().Be(1);
    }

    [Fact]
    public void UpdatePoOverallStatus_WhenNoItemsReceived_SetsStatusDoTo0()
    {
        using var trans = TransHelper.NewScope();
        var poId = NewTestPoId();
        InsertTestPo(poId, statusDo: 1);
        InsertTestPoItem(poId, 1, "1.DUMI.2", qty: 10, qtyDo: 0, isClosed: false);
        InsertTestPoItem(poId, 2, "1.DUMI.2", qty: 5, qtyDo: 0, isClosed: false);

        _sut.UpdatePoOverallStatus(poId);

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        var status = conn.ExecuteScalar<decimal>("SELECT fn_status_do FROM tb_trs_po WHERE fs_kd_trs = @PoId", new { PoId = poId });
        status.Should().Be(0);
    }

    [Fact]
    public void ListPendingPoItems_WhenDefaultDates_FiltersToDefaultWindow()
    {
        using var trans = TransHelper.NewScope();
        var recentPoId = NewTestPoId();
        var oldPoId = NewTestPoId();
        var today = DateTime.Today;

        InsertTestPo(recentPoId, tglTrs: today.AddDays(-5).ToString("yyyy-MM-dd"));
        InsertTestPoItem(recentPoId, 1, "1.DUMI.2", qty: 10, qtyDo: 2, isClosed: false);

        InsertTestPo(oldPoId, tglTrs: today.AddDays(-40).ToString("yyyy-MM-dd"));
        InsertTestPoItem(oldPoId, 1, "1.DUMI.2", qty: 10, qtyDo: 2, isClosed: false);

        var actual = _sut.ListPendingPoItems().ToList();

        actual.Should().Contain(x => x.PoId == recentPoId);
        actual.First(x => x.PoId == recentPoId).SatuanKecilId.Should().Be("BOK");
        actual.Should().NotContain(x => x.PoId == oldPoId);
    }

    [Fact]
    public void ListPendingPoItems_WhenCustomDateRangeProvided_FiltersByCustomRange()
    {
        using var trans = TransHelper.NewScope();
        var poId1 = NewTestPoId();
        var poId2 = NewTestPoId();

        InsertTestPo(poId1, tglTrs: "2025-05-10");
        InsertTestPoItem(poId1, 1, "1.DUMI.2", qty: 10, qtyDo: 0, isClosed: false);

        InsertTestPo(poId2, tglTrs: "2025-06-10");
        InsertTestPoItem(poId2, 1, "1.DUMI.2", qty: 10, qtyDo: 0, isClosed: false);

        var mayResults = _sut.ListPendingPoItems(tglAwal: "2025-05-01", tglAkhir: "2025-05-31").ToList();
        mayResults.Should().Contain(x => x.PoId == poId1);
        mayResults.Should().NotContain(x => x.PoId == poId2);

        var juneResults = _sut.ListPendingPoItems(tglAwal: "2025-06-01", tglAkhir: "2025-06-30").ToList();
        juneResults.Should().Contain(x => x.PoId == poId2);
        juneResults.Should().NotContain(x => x.PoId == poId1);
    }

    [Fact]
    public void ListPendingPoItems_WhenSearchProvided_BypassesDateWindow()
    {
        using var trans = TransHelper.NewScope();
        var oldPoId = NewTestPoId();

        // PO from 2023, outside the default 30-day window and specified 2026 window
        InsertTestPo(oldPoId, tglTrs: "2023-01-15");
        InsertTestPoItem(oldPoId, 1, "1.DUMI.2", qty: 10, qtyDo: 0, isClosed: false);

        var actual = _sut.ListPendingPoItems(tglAwal: "2026-10-01", tglAkhir: "2026-10-31", search: oldPoId).ToList();

        actual.Should().Contain(x => x.PoId == oldPoId);
    }

    [Fact]
    public void ListPendingPoItems_WhenLimitProvided_LimitsNumberOfReturnedRows()
    {
        using var trans = TransHelper.NewScope();
        var poId = NewTestPoId();

        InsertTestPo(poId, tglTrs: DateTime.Today.ToString("yyyy-MM-dd"));
        InsertTestPoItem(poId, 1, "1.DUMI.2", qty: 10, qtyDo: 0, isClosed: false);
        InsertTestPoItem(poId, 2, "1.DUMI.2", qty: 10, qtyDo: 0, isClosed: false);
        InsertTestPoItem(poId, 3, "1.DUMI.2", qty: 10, qtyDo: 0, isClosed: false);

        var actual = _sut.ListPendingPoItems(search: poId, limit: 2).ToList();

        actual.Should().HaveCount(2);
        actual.Should().OnlyContain(x => x.PoId == poId);
    }

    [Fact]
    public void ListPendingPoItems_WhenPoHasActiveDraft_ReturnsActiveDraftDoIdAndSuratJalan()
    {
        using var trans = TransHelper.NewScope();
        var poId = NewTestPoId();
        var doId = NewTestDoId();
        var suratJalanNo = $"SJ-{Random.Shared.Next(10000, 99999)}";

        InsertTestPo(poId, tglTrs: DateTime.Today.ToString("yyyy-MM-dd"));
        InsertTestPoItem(poId, 1, "1.DUMI.2", qty: 10, qtyDo: 0, isClosed: false);
        InsertTestDo(doId, poId, suratJalanNo);
        InsertTestDoInspection(doId, 1, poId, status: "Draft");

        var actual = _sut.ListPendingPoItems(search: poId).ToList();

        actual.Should().NotBeEmpty();
        var item = actual.First();
        item.PoId.Should().Be(poId);
        item.ActiveDraftDoId.Should().Be(doId);
        item.ActiveDraftSuratJalan.Should().Be(suratJalanNo);
    }

    [Fact]
    public void ListPendingPoItems_WhenPoHasNoDraft_ReturnsEmptyActiveDraftFields()
    {
        using var trans = TransHelper.NewScope();
        var poId = NewTestPoId();

        InsertTestPo(poId, tglTrs: DateTime.Today.ToString("yyyy-MM-dd"));
        InsertTestPoItem(poId, 1, "1.DUMI.2", qty: 10, qtyDo: 0, isClosed: false);

        var actual = _sut.ListPendingPoItems(search: poId).ToList();

        actual.Should().NotBeEmpty();
        var item = actual.First();
        item.PoId.Should().Be(poId);
        item.ActiveDraftDoId.Should().Be(string.Empty);
        item.ActiveDraftSuratJalan.Should().Be(string.Empty);
    }


}
