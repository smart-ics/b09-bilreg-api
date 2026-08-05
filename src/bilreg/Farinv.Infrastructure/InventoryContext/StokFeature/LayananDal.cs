using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public interface ILayananDal :
    IInsert<LayananDto>,
    IUpdate<LayananDto>,
    IDelete<ILayananKey>,
    IGetData<LayananDto, ILayananKey>,
    IListData<LayananDto>
{
}

public class LayananDal : ILayananDal
{
    private readonly DatabaseOptions _opt;
    public LayananDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(LayananDto dto)
    {
        const string sql = """
                           INSERT INTO ta_layanan(
                               fs_kd_layanan, fs_nm_layanan, fs_kd_jenis_lokasi)
                           VALUES( 
                               @fs_kd_layanan, @fs_nm_layanan, @fs_kd_jenis_lokasi)
                           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_layanan", dto.fs_nm_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_lokasi", dto.fs_kd_jenis_lokasi, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Update(LayananDto dto)
    {
        const string sql = """
                           UPDATE 
                               ta_layanan
                           SET
                               fs_nm_layanan = @fs_nm_layanan,
                               fs_kd_jenis_lokasi = @fs_kd_jenis_lokasi
                           WHERE
                               fs_kd_layanan = @fs_kd_layanan
                           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_layanan", dto.fs_nm_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_lokasi", dto.fs_kd_jenis_lokasi, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(ILayananKey key)
    {
        const string sql = """
                           DELETE FROM 
                                ta_layanan
                           WHERE
                               fs_kd_layanan = @fs_kd_layanan
                           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", key.LayananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public LayananDto GetData(ILayananKey key)
    {
        const string sql = """
                           SELECT
                               aa.fs_kd_layanan,
                               aa.fs_nm_layanan,
                               aa.fs_kd_jenis_lokasi,
                               ISNULL(bb.JenisLokasiName, '') fs_nm_jenis_lokasi
                           FROM 
                               ta_layanan aa
                               LEFT JOIN FARIN_JenisLokasi bb ON aa.fs_kd_jenis_lokasi = bb.JenisLokasiId
                           WHERE
                               aa.fs_kd_layanan = @fs_kd_layanan
                           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", key.LayananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<LayananDto>(sql, dp);
        return result;
    }
    
    public IEnumerable<LayananDto> ListData()
    {
        const string sql = """
                           SELECT
                               aa.fs_kd_layanan,
                               aa.fs_nm_layanan,
                               aa.fs_kd_jenis_lokasi,
                               ISNULL(bb.JenisLokasiName, '') fs_nm_jenis_lokasi
                           FROM 
                               ta_layanan aa
                               LEFT JOIN FARIN_JenisLokasi bb ON aa.fs_kd_jenis_lokasi = bb.JenisLokasiId
                           """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LayananDto>(sql);
    }
}