using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;

// resharper disable inconsistentnaming
public interface Itb_buku_dal:
    IInsertBulk<tb_buku_dto>,
    IDelete<Itb_buku_key>
{
    IEnumerable<tb_buku_dto> ListData<T>(T key, IEnumerable<string> listKodeDo) 
        where T: IBrgKey, ILayananKey;
}

public interface Itb_buku_layer
{
    string fs_kd_barang { get; }
    string fs_kd_layanan { get; }
    string fs_kd_do { get; }
}

public interface Itb_buku_key
{
    string fs_kd_trs { get; }
}
public class tb_buku_dal : Itb_buku_dal
{
    private readonly DatabaseOptions _opt;
    public tb_buku_dal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(IEnumerable<tb_buku_dto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();
        bcp.AddMap("fs_kd_trs", "fs_kd_trs");
        bcp.AddMap("fs_kd_barang", "fs_kd_barang");
        bcp.AddMap("fs_kd_layanan", "fs_kd_layanan");

        bcp.AddMap("fs_kd_po", "fs_kd_po");
        bcp.AddMap("fs_kd_do", "fs_kd_do");
        bcp.AddMap("fd_tgl_ed", "fd_tgl_ed");
        bcp.AddMap("fs_no_batch", "fs_no_batch");
        
        bcp.AddMap("fn_stok_in", "fn_stok_in");
        bcp.AddMap("fn_stok_out", "fn_stok_out");
        bcp.AddMap("fn_hpp", "fn_hpp");

        bcp.AddMap("fs_kd_mutasi", "fs_kd_mutasi");
        bcp.AddMap("fd_tgl_mutasi", "fd_tgl_mutasi");
        bcp.AddMap("fs_jam_mutasi", "fs_jam_mutasi");
        bcp.AddMap("fd_tgl_jam_mutasi", "fd_tgl_jam_mutasi");
        
        bcp.AddMap("fs_kd_jenis_mutasi", "fs_kd_jenis_mutasi");
        bcp.AddMap("fs_kd_satuan", "fs_kd_satuan");
        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "tb_buku";
        bcp.WriteToServer(fetched.AsDataTable());
    }
    
    public void Delete(Itb_buku_key key)
    {
        const string sql = """
            DELETE FROM
                tb_buku
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.fs_kd_trs, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    
    }
    
    public IEnumerable<tb_buku_dto> ListData<T>(T brgLyn, IEnumerable<string> listKodeDo)
        where T: IBrgKey, ILayananKey
    {
        const string sql = """
           SELECT 
               aa.fs_kd_trs, aa.fs_kd_barang, aa.fs_kd_layanan,
               aa.fs_kd_po, aa.fs_kd_do, aa.fd_tgl_ed, aa.fs_no_batch, 
               aa.fn_stok_in, aa.fn_stok_out, aa.fn_hpp, 
               aa.fs_kd_mutasi, aa.fd_tgl_mutasi, aa.fs_jam_mutasi, aa.fd_tgl_jam_mutasi, 
               aa.fs_kd_jenis_mutasi, aa.fs_kd_satuan,
               ISNULL(bb.fs_nm_barang, '') fs_nm_barang, 
               ISNULL(cc.fs_nm_layanan, '') fs_nm_layanan
           FROM 
               tb_buku aa
               LEFT JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
               LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
           WHERE
               aa.fs_kd_barang = @fs_kd_barang
               AND aa.fs_kd_layanan = @fs_kd_layanan
               AND aa.fs_kd_do = @fs_kd_do
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", brgLyn.BrgId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", brgLyn.LayananId, SqlDbType.VarChar);
        dp.Add("@fs_kd_do", listKodeDo);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<tb_buku_dto>(sql, dp);
    }
}
