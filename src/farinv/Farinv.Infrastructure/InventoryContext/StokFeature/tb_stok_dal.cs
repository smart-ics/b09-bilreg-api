using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

// resharper disable inconsistentnaming
namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public interface Itb_stok_dal :
    IInsert<tb_stok_dto>,
    IUpdate<tb_stok_dto>,
    IListData<tb_stok_dto, IBrgKey, ILayananKey>
{
    void Delete(string fs_kd_trs);
    tb_stok_dto GetData(string fs_kd_trs);
    
}

public class tb_stok_dal : Itb_stok_dal
{
    private readonly DatabaseOptions _opt;
    public tb_stok_dal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(tb_stok_dto dto)
    {
        const string sql = """
            INSERT INTO tb_stok(
                fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                fn_qty, fn_qty_in, fn_hpp,
                fd_tgl_do, fs_jam_do,
                fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi,
                fs_kd_satuan)
            VALUES( 
                @fs_kd_trs, @fs_kd_barang, @fs_kd_layanan,
                @fs_kd_po, @fs_kd_do, @fd_tgl_ed, @fs_no_batch,
                @fn_qty, @fn_qty_in, @fn_hpp,
                @fd_tgl_do, @fs_jam_do,
                @fs_kd_mutasi, @fd_tgl_mutasi, @fs_jam_mutasi,
                @fs_kd_satuan)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_po", dto.fs_kd_po, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do", dto.fs_kd_do, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_ed", dto.fd_tgl_ed, SqlDbType.VarChar);
        dp.AddParam("@fs_no_batch", dto.fs_no_batch, SqlDbType.VarChar);
        dp.AddParam("@fn_qty", dto.fn_qty, SqlDbType.Decimal);
        dp.AddParam("@fn_qty_in", dto.fn_qty_in, SqlDbType.Decimal);
        dp.AddParam("@fn_hpp", dto.fn_hpp, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_do", dto.fd_tgl_do, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_do", dto.fs_jam_do, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_mutasi", dto.fs_kd_mutasi, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_mutasi", dto.fd_tgl_mutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_mutasi", dto.fs_jam_mutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_satuan", dto.fs_kd_satuan, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Update(tb_stok_dto dto)
    {
        const string sql = """
           UPDATE 
               tb_stok
           SET
               fs_kd_barang = @fs_kd_barang,
               fs_kd_layanan = @fs_kd_layanan,
               fs_kd_po = @fs_kd_po,
               fs_kd_do = @fs_kd_do,
               fd_tgl_ed = @fd_tgl_ed,
               fs_no_batch = @fs_no_batch,
               fn_qty = @fn_qty,
               fn_qty_in = @fn_qty_in,
               fn_hpp = @fn_hpp,
               fd_tgl_do = @fd_tgl_do,
               fs_jam_do = @fs_jam_do,
               fs_kd_mutasi = @fs_kd_mutasi,
               fd_tgl_mutasi = @fd_tgl_mutasi,
               fs_jam_mutasi = @fs_jam_mutasi,
               fs_kd_satuan = @fs_kd_satuan
           WHERE
               fs_kd_trs = @fs_kd_trs
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_barang", dto.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_po", dto.fs_kd_po, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do", dto.fs_kd_do, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_ed", dto.fd_tgl_ed, SqlDbType.VarChar);
        dp.AddParam("@fs_no_batch", dto.fs_no_batch, SqlDbType.VarChar);
        dp.AddParam("@fn_qty", dto.fn_qty, SqlDbType.Decimal);
        dp.AddParam("@fn_qty_in", dto.fn_qty_in, SqlDbType.Decimal);
        dp.AddParam("@fn_hpp", dto.fn_hpp, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_do", dto.fd_tgl_do, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_do", dto.fs_jam_do, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_mutasi", dto.fs_kd_mutasi, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_mutasi", dto.fd_tgl_mutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_mutasi", dto.fs_jam_mutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_satuan", dto.fs_kd_satuan, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(string fs_kd_trs)
    {
        const string sql = """
           DELETE FROM 
                tb_stok
           WHERE
               fs_kd_trs = @fs_kd_trs
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", fs_kd_trs, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public tb_stok_dto GetData(string fs_kd_trs)
    {
        const string sql = """
           SELECT
               aa.fs_kd_trs, aa.fs_kd_barang, aa.fs_kd_layanan,
               aa.fs_kd_po, aa.fs_kd_do, aa.fd_tgl_ed, aa.fs_no_batch,
               aa.fn_qty_in, aa.fn_qty, aa.fn_hpp,
               aa.fd_tgl_do, aa.fs_jam_do, 
               aa.fs_kd_mutasi, aa.fd_tgl_mutasi, aa.fs_jam_mutasi, 
               aa.fs_kd_satuan,
               ISNULL(bb.fs_nm_barang, '') fs_nm_barang,
               ISNULL(cc.fs_nm_layanan, '') fs_nm_layanan
           FROM 
               tb_stok aa
                LEFT JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
                LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
           WHERE
               aa.fs_kd_trs = @fs_kd_trs
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", fs_kd_trs, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<tb_stok_dto>(sql, dp);
        return result;
    }
    
    public IEnumerable<tb_stok_dto> ListData(IBrgKey brgFilter, ILayananKey layananFilter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs, aa.fs_kd_barang, aa.fs_kd_layanan,
                aa.fs_kd_po, aa.fs_kd_do, aa.fd_tgl_ed, aa.fs_no_batch,
                aa.fn_qty_in, aa.fn_qty, aa.fn_hpp,
                aa.fd_tgl_do, aa.fs_jam_do,
                aa.fs_kd_mutasi, aa.fd_tgl_mutasi, aa.fs_jam_mutasi,
                aa.fs_kd_satuan,
                ISNULL(bb.fs_nm_barang, '') fs_nm_barang,
                ISNULL(cc.fs_nm_layanan, '') fs_nm_layanan
            FROM 
                tb_stok aa
                LEFT JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
                LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
            WHERE
                aa.fs_kd_barang = @fs_kd_barang
                AND aa.fs_kd_layanan = @fs_kd_layanan
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", brgFilter.BrgId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", layananFilter.LayananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<tb_stok_dto>(sql, dp);
    }
}