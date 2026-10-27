using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public class PoDeliveryTrackingDal : IPoDeliveryTrackingDal
{
    private readonly DatabaseOptions _opt;

    public PoDeliveryTrackingDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<PoQuotaDto> GetRemainingQuota(string poId)
    {
        const string sql = """
            SELECT
                po2.fs_kd_trs AS PoId,
                CAST(po2.fn_no_urut AS INT) AS NoUrut,
                po2.fs_kd_barang AS BrgId,
                ISNULL(brg.fs_nm_barang, '') AS BrgName,
                po2.fs_kd_satuan AS SatuanId,
                po2.fn_qty AS QtyPo,
                po2.fn_qty_do AS QtyDo,
                (po2.fn_qty - po2.fn_qty_do) AS SisaQty,
                po2.fb_closed_po AS IsClosed,
                po2.fn_konversi AS Konversi,
                po2.fn_harga AS Harga,
                po2.fn_diskon AS Diskon
            FROM tb_trs_po2 po2
            JOIN tb_trs_po po ON po2.fs_kd_trs = po.fs_kd_trs
            LEFT JOIN tb_barang brg ON po2.fs_kd_barang = brg.fs_kd_barang
            WHERE po2.fs_kd_trs = @PoId AND po2.fb_void = 0
            ORDER BY po2.fn_no_urut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PoId", poId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PoQuotaDto>(sql, dp);
    }

    public void IncrementReceivedQty(string poId, int noUrut, decimal qtyAccepted)
    {
        const string sql = """
            UPDATE tb_trs_po2
            SET 
                fn_qty_do = fn_qty_do + @QtyAccepted,
                fb_closed_po = CASE WHEN (fn_qty_do + @QtyAccepted) >= fn_qty THEN 1 ELSE fb_closed_po END
            WHERE fs_kd_trs = @PoId AND fn_no_urut = @NoUrut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PoId", poId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);
        dp.AddParam("@QtyAccepted", qtyAccepted, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void UpdatePoOverallStatus(string poId)
    {
        const string sql = """
            UPDATE tb_trs_po
            SET fn_status_do = CASE
                WHEN NOT EXISTS (
                    SELECT 1 FROM tb_trs_po2
                    WHERE fs_kd_trs = @PoId AND fb_void = 0 AND fb_closed_po = 0
                ) AND EXISTS (
                    SELECT 1 FROM tb_trs_po2
                    WHERE fs_kd_trs = @PoId AND fb_void = 0
                ) THEN 2
                WHEN EXISTS (
                    SELECT 1 FROM tb_trs_po2
                    WHERE fs_kd_trs = @PoId AND fb_void = 0 AND (fn_qty_do > 0 OR fb_closed_po = 1)
                ) THEN 1
                ELSE 0
            END
            WHERE fs_kd_trs = @PoId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PoId", poId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PoPendingItemDto> ListPendingPoItems(string? tglAwal = null, string? tglAkhir = null, string? search = null, int limit = 100)
    {
        var effectiveTglAwal = string.IsNullOrWhiteSpace(tglAwal)
            ? DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd")
            : tglAwal.Trim();
        var effectiveTglAkhir = string.IsNullOrWhiteSpace(tglAkhir)
            ? DateTime.Today.ToString("yyyy-MM-dd")
            : tglAkhir.Trim();
        var effectiveLimit = limit <= 0 ? 100 : limit;
        var searchParam = (search ?? string.Empty).Trim();
        var searchPattern = $"%{searchParam}%";

        const string sql = """
            SELECT TOP (@Limit)
                po.fs_kd_trs AS PoId,
                po.fd_tgl_trs AS PoDate,
                po.fs_kd_iii AS SupplierId,
                ISNULL(sup.fs_nm_iii, '') AS SupplierName,
                po.fs_keterangan AS Keterangan,
                po.fn_status_do AS StatusDo,
                ISNULL(active_do.ActiveDraftDoId, '') AS ActiveDraftDoId,
                ISNULL(active_do.ActiveDraftSuratJalan, '') AS ActiveDraftSuratJalan,
                CAST(po2.fn_no_urut AS INT) AS NoUrut,
                po2.fs_kd_barang AS BrgId,
                ISNULL(brg.fs_nm_barang, '') AS BrgName,
                po2.fs_kd_satuan AS SatuanId,
                ISNULL(sat.fs_nm_satuan, '') AS SatuanName,
                po2.fn_qty AS QtyPo,
                po2.fn_qty_do AS QtyDo,
                (po2.fn_qty - po2.fn_qty_do) AS SisaQty,
                po2.fn_konversi AS Konversi,
                po2.fn_harga AS Harga,
                po2.fn_diskon AS Diskon
            FROM tb_trs_po po
            JOIN tb_trs_po2 po2 ON po.fs_kd_trs = po2.fs_kd_trs
            LEFT JOIN t_iii sup ON po.fs_kd_iii = sup.fs_kd_iii
            LEFT JOIN tb_barang brg ON po2.fs_kd_barang = brg.fs_kd_barang
            LEFT JOIN tb_satuan sat ON po2.fs_kd_satuan = sat.fs_kd_satuan
            OUTER APPLY (
                SELECT TOP 1 do.fs_kd_trs AS ActiveDraftDoId, do.fs_kd_do_asli AS ActiveDraftSuratJalan
                FROM TB_TRS_DO do
                JOIN BILRG_DoInspeksi insp ON do.fs_kd_trs = insp.DoId
                WHERE do.fs_kd_po = po.fs_kd_trs
                  AND do.fd_tgl_void = '3000-01-01'
                  AND insp.[Status] = 'Draft'
                ORDER BY do.CRTTGL DESC, do.fs_kd_trs DESC
            ) active_do
            WHERE po.fd_tgl_void = '3000-01-01'
              AND po.fb_closed = 0
              AND po.fn_status_do IN (0, 1)
              AND po2.fb_void = 0
              AND po2.fb_closed_po = 0
              AND (po2.fn_qty - po2.fn_qty_do) > 0
              AND (
                  (@Search = '' AND po.fd_tgl_trs >= @TglAwal AND po.fd_tgl_trs <= @TglAkhir)
                  OR
                  (@Search <> '' AND (po.fs_kd_trs LIKE @SearchPattern OR sup.fs_nm_iii LIKE @SearchPattern))
              )
            ORDER BY po.fd_tgl_trs DESC, po.fs_kd_trs DESC, po2.fn_no_urut ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@Limit", effectiveLimit, SqlDbType.Int);
        dp.AddParam("@TglAwal", effectiveTglAwal, SqlDbType.VarChar);
        dp.AddParam("@TglAkhir", effectiveTglAkhir, SqlDbType.VarChar);
        dp.AddParam("@Search", searchParam, SqlDbType.VarChar);
        dp.AddParam("@SearchPattern", searchPattern, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PoPendingItemDto>(sql, dp) ?? Enumerable.Empty<PoPendingItemDto>();
    }
}
