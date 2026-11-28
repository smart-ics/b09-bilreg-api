using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.LogistikMedisFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.LogistikMedisFeature;

public class LogistikMedisBffDal : ILogistikMedisBffDal
{
    private readonly DatabaseOptions _opt;

    public LogistikMedisBffDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<LogistikMedisStokBatchRowDto> ListStokBatch(string layananId)
    {
        const string sql = """
            SELECT
                aa.fs_kd_barang AS BrgId,
                bb.fs_nm_barang AS BrgName,
                aa.fs_kd_satuan AS SatuanId,
                ISNULL(dd.fs_nm_satuan, '') AS SatuanName,
                ISNULL(cc.fs_kd_grup_rek_dk, '') AS GroupRekDkId,
                aa.fs_no_batch AS NoBatch,
                aa.fd_tgl_ed AS TglEd,
                aa.fn_hpp AS Hpp,
                aa.fn_qty AS QtySisa
            FROM tb_stok aa
            INNER JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
            LEFT JOIN tb_satuan dd ON aa.fs_kd_satuan = dd.fs_kd_satuan
            LEFT JOIN tb_grup_rek cc ON bb.fs_kd_grup_rek = cc.fs_kd_grup_rek
            WHERE aa.fs_kd_layanan = @layananId
              AND aa.fn_qty > 0
              AND bb.fb_aktif = 1
            ORDER BY aa.fd_tgl_ed ASC, aa.fs_kd_trs ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@layananId", layananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisStokBatchRowDto>(sql, dp);
    }

    public IEnumerable<LogistikMedisBarangLayananRowDto> ListBarangLayanan(string layananId)
    {
        const string sql = """
            SELECT
                aa.fs_kd_barang AS BrgId,
                bb.fs_nm_barang AS BrgName,
                ISNULL(st.fs_kd_satuan, '') AS SatuanId,
                ISNULL(st.fs_nm_satuan, '') AS SatuanName,
                ISNULL(cc.fs_kd_grup_rek_dk, '') AS GroupRekDkId,
                aa.fn_stok_min AS MinStok,
                aa.fn_stok_max AS MaxStok,
                aa.fs_kd_rak AS KdRak,
                aa.fs_ket_lokasi AS KetLokasi
            FROM tb_barang_layanan aa
            INNER JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
            LEFT JOIN tb_grup_rek cc ON bb.fs_kd_grup_rek = cc.fs_kd_grup_rek
            OUTER APPLY (
                SELECT TOP 1 bs.fs_kd_satuan, s.fs_nm_satuan
                FROM tb_barang_satuan bs
                LEFT JOIN tb_satuan s ON bs.fs_kd_satuan = s.fs_kd_satuan
                WHERE bs.fs_kd_barang = aa.fs_kd_barang
                ORDER BY bs.fn_nilai ASC
            ) st
            WHERE aa.fs_kd_layanan = @layananId
              AND bb.fb_aktif = 1
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@layananId", layananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisBarangLayananRowDto>(sql, dp);
    }

    public IEnumerable<LogistikMedisAggQtyRowDto> ListPendingOrderQty(string layananId)
    {
        const string sql = """
            SELECT
                b.BrgId,
                br.fs_nm_barang AS BrgName,
                MAX(ISNULL(NULLIF(b.SatuanId, ''), ISNULL(st.fs_kd_satuan, ''))) AS SatuanId,
                MAX(ISNULL(NULLIF(b.SatuanName, ''), ISNULL(st.fs_nm_satuan, ''))) AS SatuanName,
                ISNULL(gr.fs_kd_grup_rek_dk, '') AS GroupRekDkId,
                SUM(CASE WHEN b.Qty > b.QtyTerpenuhi THEN b.Qty - b.QtyTerpenuhi ELSE 0 END) AS Qty
            FROM FARIN_OrderMutasi a
            INNER JOIN FARIN_OrderMutasiItem b ON a.OrderMutasiId = b.OrderMutasiId
            INNER JOIN tb_barang br ON b.BrgId = br.fs_kd_barang
            LEFT JOIN tb_grup_rek gr ON br.fs_kd_grup_rek = gr.fs_kd_grup_rek
            OUTER APPLY (
                SELECT TOP 1 bs.fs_kd_satuan, s.fs_nm_satuan
                FROM tb_barang_satuan bs
                LEFT JOIN tb_satuan s ON bs.fs_kd_satuan = s.fs_kd_satuan
                WHERE bs.fs_kd_barang = b.BrgId
                ORDER BY bs.fn_nilai ASC
            ) st
            WHERE a.LayananOrderId = @layananId
              AND a.State IN (1, 2, 5)
              AND br.fb_aktif = 1
            GROUP BY b.BrgId, br.fs_nm_barang, ISNULL(gr.fs_kd_grup_rek_dk, '')
            HAVING SUM(CASE WHEN b.Qty > b.QtyTerpenuhi THEN b.Qty - b.QtyTerpenuhi ELSE 0 END) > 0
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@layananId", layananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisAggQtyRowDto>(sql, dp);
    }

    public IEnumerable<LogistikMedisAggQtyRowDto> ListIncomingMutasiQty(string layananId)
    {
        const string sql = """
            SELECT
                b.BrgId,
                br.fs_nm_barang AS BrgName,
                MAX(ISNULL(NULLIF(b.SatuanId, ''), ISNULL(st.fs_kd_satuan, ''))) AS SatuanId,
                MAX(ISNULL(NULLIF(b.SatuanName, ''), ISNULL(st.fs_nm_satuan, ''))) AS SatuanName,
                ISNULL(gr.fs_kd_grup_rek_dk, '') AS GroupRekDkId,
                SUM(b.QtyKirim) AS Qty
            FROM BILRG_MutasiBrg a
            INNER JOIN BILRG_MutasiBrgItem b ON a.MutasiBrgId = b.MutasiBrgId
            INNER JOIN tb_barang br ON b.BrgId = br.fs_kd_barang
            LEFT JOIN tb_grup_rek gr ON br.fs_kd_grup_rek = gr.fs_kd_grup_rek
            OUTER APPLY (
                SELECT TOP 1 bs.fs_kd_satuan, s.fs_nm_satuan
                FROM tb_barang_satuan bs
                LEFT JOIN tb_satuan s ON bs.fs_kd_satuan = s.fs_kd_satuan
                WHERE bs.fs_kd_barang = b.BrgId
                ORDER BY bs.fn_nilai ASC
            ) st
            WHERE a.LayananTujuanId = @layananId
              AND a.State = 1
              AND br.fb_aktif = 1
            GROUP BY b.BrgId, br.fs_nm_barang, ISNULL(gr.fs_kd_grup_rek_dk, '')
            HAVING SUM(b.QtyKirim) > 0
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@layananId", layananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisAggQtyRowDto>(sql, dp);
    }

    public IEnumerable<LogistikMedisOrderMutasiHeaderRowDto> ListOrderMutasiByLayananOrder(
        string layananOrderId,
        DateTime tgl1,
        DateTime tgl2)
    {
        const string sql = """
            SELECT
                OrderMutasiId,
                OrderMutasiDate,
                State,
                Priority,
                LayananOrderId,
                LayananOrderName,
                LayananTujuanId,
                LayananTujuanName,
                ISNULL(OrderNote, '') AS OrderNote
            FROM FARIN_OrderMutasi
            WHERE LayananOrderId = @LayananOrderId
              AND OrderMutasiDate BETWEEN @Tgl1 AND @Tgl2
              AND State <> 0
            ORDER BY OrderMutasiDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@LayananOrderId", layananOrderId, SqlDbType.VarChar);
        dp.AddParam("@Tgl1", tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisOrderMutasiHeaderRowDto>(sql, dp) ?? [];
    }

    public IEnumerable<LogistikMedisOrderMutasiItemRowDto> ListOrderMutasiItems(IEnumerable<string> orderMutasiIds)
    {
        var ids = NormalizeIds(orderMutasiIds);
        if (ids.Count == 0)
            return [];

        const string sql = """
            SELECT
                OrderMutasiId,
                NoUrut,
                BrgId,
                BrgName,
                StokCurrent,
                Qty,
                QtyTerpenuhi,
                SatuanId,
                SatuanName
            FROM FARIN_OrderMutasiItem
            WHERE OrderMutasiId IN @Ids
            ORDER BY OrderMutasiId, NoUrut
            """;

        var dp = new DynamicParameters();
        dp.Add("@Ids", ids);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisOrderMutasiItemRowDto>(sql, dp) ?? [];
    }

    public IEnumerable<LogistikMedisMutasiBrgHeaderRowDto> ListMutasiBrgInTransit(string layananTujuanId)
    {
        const string sql = """
            SELECT
                MutasiBrgId,
                MutasiBrgDate,
                State,
                LayananAsalId,
                LayananAsalName,
                LayananTujuanId,
                LayananTujuanName,
                ISNULL(OrderMutasiId, '') AS OrderMutasiId,
                ISNULL(DispatchNote, '') AS DispatchNote,
                IssuerUserId,
                ISNULL(CancelReason, '') AS CancelReason
            FROM BILRG_MutasiBrg
            WHERE LayananTujuanId = @LayananTujuanId
              AND State = 1
            ORDER BY MutasiBrgDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@LayananTujuanId", layananTujuanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisMutasiBrgHeaderRowDto>(sql, dp) ?? [];
    }

    public IEnumerable<LogistikMedisMutasiBrgHeaderRowDto> ListMutasiBrgKeluar(
        string layananAsalId,
        DateTime tgl1,
        DateTime tgl2)
    {
        const string sql = """
            SELECT
                MutasiBrgId,
                MutasiBrgDate,
                State,
                LayananAsalId,
                LayananAsalName,
                LayananTujuanId,
                LayananTujuanName,
                ISNULL(OrderMutasiId, '') AS OrderMutasiId,
                ISNULL(DispatchNote, '') AS DispatchNote,
                IssuerUserId,
                ISNULL(CancelReason, '') AS CancelReason
            FROM BILRG_MutasiBrg
            WHERE LayananAsalId = @LayananAsalId
              AND (MutasiBrgDate BETWEEN @Tgl1 AND @Tgl2 OR State = 1)
            ORDER BY MutasiBrgDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@LayananAsalId", layananAsalId, SqlDbType.VarChar);
        dp.AddParam("@Tgl1", tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisMutasiBrgHeaderRowDto>(sql, dp) ?? [];
    }

    public IEnumerable<LogistikMedisMutasiBrgItemRowDto> ListMutasiBrgItems(IEnumerable<string> mutasiBrgIds)
    {
        var ids = NormalizeIds(mutasiBrgIds);
        if (ids.Count == 0)
            return [];

        const string sql = """
            SELECT
                MutasiBrgId,
                NoUrut,
                BrgId,
                BrgName,
                ISNULL(BatchNo, '') AS BatchNo,
                ISNULL(ExpiredDate, '') AS ExpiredDate,
                QtyKirim,
                SatuanId,
                SatuanName
            FROM BILRG_MutasiBrgItem
            WHERE MutasiBrgId IN @Ids
            ORDER BY MutasiBrgId, NoUrut
            """;

        var dp = new DynamicParameters();
        dp.Add("@Ids", ids);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisMutasiBrgItemRowDto>(sql, dp) ?? [];
    }

    public IEnumerable<LogistikMedisTerimaMutasiHeaderRowDto> ListTerimaMutasi(
        string layananTujuanId,
        DateTime tgl1,
        DateTime tgl2)
    {
        const string sql = """
            SELECT
                TerimaMutasiId,
                TerimaMutasiDate,
                State,
                MutasiBrgId,
                ISNULL(OrderMutasiId, '') AS OrderMutasiId,
                LayananAsalId,
                LayananAsalName,
                LayananTujuanId,
                LayananTujuanName,
                ReceiverUserId,
                ISNULL(Note, '') AS Note
            FROM BILRG_TerimaMutasi
            WHERE LayananTujuanId = @LayananTujuanId
              AND TerimaMutasiDate BETWEEN @Tgl1 AND @Tgl2
            ORDER BY TerimaMutasiDate DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@LayananTujuanId", layananTujuanId, SqlDbType.VarChar);
        dp.AddParam("@Tgl1", tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisTerimaMutasiHeaderRowDto>(sql, dp) ?? [];
    }

    public IEnumerable<LogistikMedisTerimaMutasiItemRowDto> ListTerimaMutasiItems(IEnumerable<string> terimaMutasiIds)
    {
        var ids = NormalizeIds(terimaMutasiIds);
        if (ids.Count == 0)
            return [];

        const string sql = """
            SELECT
                TerimaMutasiId,
                NoUrut,
                BrgId,
                BrgName,
                ISNULL(BatchNo, '') AS BatchNo,
                ISNULL(ExpiredDate, '') AS ExpiredDate,
                QtyTerima,
                SatuanId,
                SatuanName
            FROM BILRG_TerimaMutasiItem
            WHERE TerimaMutasiId IN @Ids
            ORDER BY TerimaMutasiId, NoUrut
            """;

        var dp = new DynamicParameters();
        dp.Add("@Ids", ids);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LogistikMedisTerimaMutasiItemRowDto>(sql, dp) ?? [];
    }

    private static List<string> NormalizeIds(IEnumerable<string>? rawIds)
    {
        return rawIds?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }
}
