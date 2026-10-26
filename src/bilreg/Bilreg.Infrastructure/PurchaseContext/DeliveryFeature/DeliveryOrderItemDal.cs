using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public interface IDeliveryOrderItemDal :
    IInsert<DeliveryOrderItemDto>,
    IInsertBulk<DeliveryOrderItemDto>,
    IDelete<IDeliveryOrderKey>,
    IListData<DeliveryOrderItemDto, IDeliveryOrderKey>
{
}

public class DeliveryOrderItemDal : IDeliveryOrderItemDal
{
    private const string DefaultVoidDate = "3000-01-01";
    private const string DefaultVoidTime = "00:00:00";
    private readonly DatabaseOptions _opt;

    public DeliveryOrderItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(DeliveryOrderItemDto dto)
    {
        const string sql = """
            INSERT INTO TB_TRS_DO2 (
                fs_kd_trs, fn_no_urut, fs_kd_barang, fn_harga, fn_diskon,
                fn_qty, fs_kd_satuan, fn_sub_total, fn_tax, fn_total,
                fs_no_batch, fd_tgl_ed, fb_void, fn_het, fn_biaya,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR,
                fn_konversi
            ) VALUES (
                @fs_kd_trs, @fn_no_urut, @fs_kd_barang, @fn_harga, @fn_diskon,
                @fn_qty, @fs_kd_satuan, @fn_sub_total, @fn_tax, @fn_total,
                @fs_no_batch, @fd_tgl_ed, @fb_void, @fn_het, @fn_biaya,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR,
                @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR,
                @fn_konversi
            )
            """;

        var dp = BuildWriteParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Insert(IEnumerable<DeliveryOrderItemDto> listModel)
    {
        var items = listModel?.ToList() ?? new List<DeliveryOrderItemDto>();
        if (items.Count == 0) return;

        const string sql = """
            INSERT INTO TB_TRS_DO2 (
                fs_kd_trs, fn_no_urut, fs_kd_barang, fn_harga, fn_diskon,
                fn_qty, fs_kd_satuan, fn_sub_total, fn_tax, fn_total,
                fs_no_batch, fd_tgl_ed, fb_void, fn_het, fn_biaya,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR,
                fn_konversi
            ) VALUES (
                @fs_kd_trs, @fn_no_urut, @fs_kd_barang, @fn_harga, @fn_diskon,
                @fn_qty, @fs_kd_satuan, @fn_sub_total, @fn_tax, @fn_total,
                @fs_no_batch, @fd_tgl_ed, @fb_void, @fn_het, @fn_biaya,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR,
                @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR,
                @fn_konversi
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        foreach (var item in items)
        {
            var dp = BuildWriteParams(item);
            conn.Execute(sql, dp);
        }
    }

    public void Delete(IDeliveryOrderKey key)
    {
        const string sql = """
            DELETE FROM TB_TRS_DO2
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<DeliveryOrderItemDto> ListData(IDeliveryOrderKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs AS DoId,
                CAST(aa.fn_no_urut AS INT) AS NoUrut,
                aa.fs_kd_barang AS BrgId,
                ISNULL(bb.fs_nm_barang, '') AS BrgName,
                aa.fn_harga AS Harga,
                aa.fn_diskon AS Diskon,
                aa.fn_qty AS Qty,
                aa.fs_kd_satuan AS SatuanId,
                ISNULL(cc.fs_nm_satuan, '') AS SatuanName,
                aa.fn_sub_total AS SubTotal,
                aa.fn_tax AS Tax,
                aa.fn_total AS Total,
                aa.fs_no_batch AS NoBatch,
                CASE WHEN ISDATE(aa.fd_tgl_ed) = 1 THEN CONVERT(DATETIME, aa.fd_tgl_ed) ELSE '3000-01-01' END AS TglEd,
                aa.fb_void AS IsVoid,
                CAST(aa.fn_konversi AS INT) AS Konversi,
                aa.fn_het AS Het,
                aa.fn_biaya AS Biaya
            FROM TB_TRS_DO2 aa
            LEFT JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
            LEFT JOIN tb_satuan cc ON aa.fs_kd_satuan = cc.fs_kd_satuan
            WHERE aa.fs_kd_trs = @fs_kd_trs
            ORDER BY aa.fn_no_urut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", filter.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<DeliveryOrderItemDto>(sql, dp) ?? Enumerable.Empty<DeliveryOrderItemDto>();
    }

    private static DynamicParameters BuildWriteParams(DeliveryOrderItemDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.DoId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fn_no_urut", dto.NoUrut, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_barang", dto.BrgId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fn_harga", dto.Harga, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", dto.Diskon, SqlDbType.Decimal);
        dp.AddParam("@fn_qty", dto.Qty, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_satuan", dto.SatuanId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fn_sub_total", dto.SubTotal, SqlDbType.Decimal);
        dp.AddParam("@fn_tax", dto.Tax, SqlDbType.Decimal);
        dp.AddParam("@fn_total", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@fs_no_batch", dto.NoBatch ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_ed", dto.TglEd != default ? dto.TglEd.ToString("yyyy-MM-dd") : DefaultVoidDate, SqlDbType.VarChar);
        dp.AddParam("@fb_void", dto.IsVoid, SqlDbType.Bit);
        dp.AddParam("@fn_het", dto.Het, SqlDbType.Decimal);
        dp.AddParam("@fn_biaya", dto.Biaya, SqlDbType.Decimal);
        dp.AddParam("@CRTTGL", DateTime.Today.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@CRTJAM", DateTime.Now.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@CRTIPA", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CRTVER", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", DefaultVoidDate, SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", DefaultVoidTime, SqlDbType.VarChar);
        dp.AddParam("@UPDIPA", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UPDVER", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fn_konversi", dto.Konversi, SqlDbType.Decimal);
        return dp;
    }
}
