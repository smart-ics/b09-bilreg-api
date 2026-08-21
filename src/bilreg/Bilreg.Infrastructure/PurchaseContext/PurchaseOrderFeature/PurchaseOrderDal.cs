using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PurchaseContext.PurchaseOrderFeature;

public interface IPurchaseOrderDal :
    IInsert<PurchaseOrderDto>,
    IUpdate<PurchaseOrderDto>,
    IDelete<IPurchaseOrderKey>,
    IGetData<PurchaseOrderDto, IPurchaseOrderKey>,
    IListData<PurchaseOrderDto, Periode>;

public class PurchaseOrderDal: IPurchaseOrderDal
{
    private const string ApiUser = "BILREG-API";
    private readonly DatabaseOptions _opt;

    public PurchaseOrderDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(PurchaseOrderDto model)
    {
        const string sql = """
            INSERT INTO tb_trs_po (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fs_keterangan,
                fs_kd_iii, fn_sub_total, fn_tax_rupiah, fn_total, fn_diskon_lain,
                fn_biaya_lain, fn_grand_total, fd_tgl_void, fs_jam_void,
                fs_kd_petugas_void, CRTUSR)
            VALUES (
                @fs_kd_trs, @fd_tgl_trs, @fs_jam_trs, @fs_kd_petugas, @fs_keterangan,
                @fs_kd_iii, @fn_sub_total, @fn_tax_rupiah, @fn_total, @fn_diskon_lain,
                @fn_biaya_lain, @fn_grand_total, @fd_tgl_void, @fs_jam_void,
                @fs_kd_petugas_void, @CRTUSR)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", model.PurchaseOrderId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", model.TglTrs, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", model.JamTrs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", model.UserId, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan", model.Keterangan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_iii", model.PartnerId, SqlDbType.VarChar);
        dp.AddParam("@fn_sub_total", model.SubTotal, SqlDbType.Decimal);
        dp.AddParam("@fn_tax_rupiah", model.TaxTotal, SqlDbType.Decimal);
        dp.AddParam("@fn_total", model.Total, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon_lain", model.DiskonLain, SqlDbType.Decimal);
        dp.AddParam("@fn_biaya_lain", model.BiayaLain, SqlDbType.Decimal);
        dp.AddParam("@fn_grand_total", model.GrandTotal, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_void", model.TglVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", model.JamVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", model.UserVoidId, SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", ApiUser, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PurchaseOrderDto model)
    {
        const string sql = """
           UPDATE tb_trs_po
           SET 
               fd_tgl_trs = @fd_tgl_trs, 
               fs_jam_trs = @fs_jam_trs, 
               fs_kd_petugas = @fs_kd_petugas, 
               fs_keterangan = @fs_keterangan,
               fs_kd_iii = @fs_kd_iii, 
               fn_sub_total = @fn_sub_total, 
               fn_tax_rupiah = @fn_tax_rupiah, 
               fn_total = @fn_total, 
               fn_diskon_lain = @fn_diskon_lain,
               fn_biaya_lain = @fn_biaya_lain, 
               fn_grand_total = @fn_grand_total, 
               fd_tgl_void = @fd_tgl_void, 
               fs_jam_void = @fs_jam_void,
               fs_kd_petugas_void = @fs_kd_petugas_void, 
               UPDUSER = @UPDUSER
           WHERE
               fs_kd_trs = @fs_kd_trs
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", model.PurchaseOrderId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", model.TglTrs, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", model.JamTrs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", model.UserId, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan", model.Keterangan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_iii", model.PartnerId, SqlDbType.VarChar);
        dp.AddParam("@fn_sub_total", model.SubTotal, SqlDbType.Decimal);
        dp.AddParam("@fn_tax_rupiah", model.TaxTotal, SqlDbType.Decimal);
        dp.AddParam("@fn_total", model.Total, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon_lain", model.DiskonLain, SqlDbType.Decimal);
        dp.AddParam("@fn_biaya_lain", model.BiayaLain, SqlDbType.Decimal);
        dp.AddParam("@fn_grand_total", model.GrandTotal, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_void", model.TglVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", model.JamVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", model.UserVoidId, SqlDbType.VarChar);
        dp.AddParam("@UPDUSER", ApiUser, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPurchaseOrderKey key)
    {
        const string sql = """
           DELETE FROM tb_trs_po
           WHERE fs_kd_trs = @fs_kd_trs
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.PurchaseOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PurchaseOrderDto GetData(IPurchaseOrderKey key)
    {
        var sql = $"""
           {SelectClause()}
           WHERE
               aa.fs_kd_trs = @PurchaseOrderId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@PurchaseOrderId", key.PurchaseOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PurchaseOrderDto>(sql, dp);
    }

    public IEnumerable<PurchaseOrderDto> ListData(Periode filter)
    {
        var sql = $"""
           {SelectClause()}
           WHERE
                aa.fd_tgl_trs BETWEEN @Tgl1 AND @Tgl2
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PurchaseOrderDto>(sql, dp);
    }
    
    private static string SelectClause() => """
        SELECT
            aa.fs_kd_trs AS PurchaseOrderId,
            aa.fd_tgl_trs AS TglTrs,
            aa.fs_jam_trs AS JamTrs,
            aa.fs_keterangan AS Keterangan,
            aa.fs_kd_iii AS PartnerId,
            aa.fn_sub_total AS SubTotal,
            aa.fn_tax_rupiah AS TaxTotal,
            aa.fn_total AS Total,
            aa.fn_diskon_lain AS DiskonLain,
            aa.fn_biaya_lain AS BiayaLain,
            aa.fn_grand_total AS GrandTotal,
            aa.fs_kd_petugas AS UserId,
            aa.fd_tgl_void AS TglVoid,
            aa.fs_jam_void AS JamVoid,
            aa.fs_kd_petugas_void AS UserVoidId,
            ISNULL(bb.fs_nm_iii, '') AS PartnerName
        FROM
            tb_trs_po aa
            LEFT JOIN t_iii bb ON aa.fs_kd_iii = bb.fs_kd_iii
        """;
}