using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.LayananFeature.LayananAgg;

public interface ILayananDal :
    IInsert<LayananDto>,
    IUpdate<LayananDto>,
    IDelete<ILayananKey>,
    IGetData<LayananDto, ILayananKey>,
    IListData<LayananDto>,
    IListData<LayananDto, IInstalasiDkKey>
{ }

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
            INSERT INTO ta_layanan (
                fs_kd_layanan, fs_nm_layanan, fb_aktif, 
                fs_kd_instalasi, fs_kd_layanan_dk,fs_kd_layanan_tipe_dk) 
            VALUES (
                @fs_kd_layanan, @fs_nm_layanan, @fb_aktif, 
                @fs_kd_instalasi, @fs_kd_layanan_dk,@fs_kd_layanan_tipe_dk) 
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_layanan", dto.fs_nm_layanan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_instalasi", dto.fs_kd_instalasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan_dk", dto.fs_kd_layanan_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan_tipe_dk", dto.fs_kd_layanan_tipe_dk, SqlDbType.VarChar);

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
                fb_aktif = @fb_aktif,  
                fs_kd_instalasi = @fs_kd_instalasi,  
                fs_kd_layanan_dk = @fs_kd_layanan_dk,  
                fs_kd_layanan_tipe_dk = @fs_kd_layanan_tipe_dk 
            WHERE     
                fs_kd_layanan = @fs_kd_layanan
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_layanan", dto.fs_nm_layanan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_instalasi", dto.fs_kd_instalasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan_dk", dto.fs_kd_layanan_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan_tipe_dk", dto.fs_kd_layanan_tipe_dk, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ILayananKey key)
    {
        const string sql = """
            DELETE FROM ta_layanan
            WHERE fs_kd_layanan = @fs_kd_layanan 
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
               aa.fs_kd_layanan, aa.fs_nm_layanan, aa.fb_aktif,
               aa.fs_kd_instalasi, aa.fs_kd_layanan_dk,
               aa.fs_kd_layanan_tipe_dk, aa.GroupSpesialisId,
               ISNULL(bb.fs_nm_instalasi,'') AS fs_nm_instalasi,
               ISNULL(bb.fs_kd_instalasi_dk, '') AS fs_kd_instalasi_dk,
               ISNULL(cc.fs_nm_layanan_dk,'') AS fs_nm_layanan_dk,
               ISNULL(dd.fs_nm_layanan_tipe_dk,'') AS fs_nm_layanan_tipe_dk,
               ISNULL(ee.fs_nm_instalasi_dk, '') AS fs_nm_instalasi_dk,
               ISNULL(ff.GroupSpesialisName, '') AS GroupSpesialisName
           FROM 
               ta_layanan aa
               LEFT JOIN ta_instalasi bb ON aa.fs_kd_instalasi = bb.fs_kd_instalasi
               LEFT JOIN ta_layanan_dk cc ON aa.fs_kd_layanan_dk = cc.fs_kd_layanan_dk
               LEFT JOIN ta_layanan_tipe_dk dd ON aa.fs_kd_layanan_tipe_dk = dd.fs_kd_layanan_tipe_dk
               LEFT JOIN ta_instalasi_dk ee ON bb.fs_kd_instalasi_dk = ee.fs_kd_instalasi_dk
               LEFT JOIN BILRG_GroupSpesialis ff ON aa.GroupSpesialisId = ff.GroupSpesialisId
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
               aa.fs_kd_layanan, aa.fs_nm_layanan, aa.fb_aktif,
               aa.fs_kd_instalasi, aa.fs_kd_layanan_dk,
               aa.fs_kd_layanan_tipe_dk, aa.GroupSpesialisId,
               ISNULL(bb.fs_nm_instalasi,'') AS fs_nm_instalasi,
               ISNULL(bb.fs_kd_instalasi_dk, '') AS fs_kd_instalasi_dk,
               ISNULL(cc.fs_nm_layanan_dk,'') AS fs_nm_layanan_dk,
               ISNULL(dd.fs_nm_layanan_tipe_dk,'') AS fs_nm_layanan_tipe_dk,
               ISNULL(ee.fs_nm_instalasi_dk, '') AS fs_nm_instalasi_dk,
               ISNULL(ff.GroupSpesialisName, '') AS GroupSpesialisName
           FROM 
               ta_layanan aa
               LEFT JOIN ta_instalasi bb ON aa.fs_kd_instalasi = bb.fs_kd_instalasi
               LEFT JOIN ta_layanan_dk cc ON aa.fs_kd_layanan_dk = cc.fs_kd_layanan_dk
               LEFT JOIN ta_layanan_tipe_dk dd ON aa.fs_kd_layanan_tipe_dk = dd.fs_kd_layanan_tipe_dk
               LEFT JOIN ta_instalasi_dk ee ON bb.fs_kd_instalasi_dk = ee.fs_kd_instalasi_dk
               LEFT JOIN BILRG_GroupSpesialis ff ON aa.GroupSpesialisId = ff.GroupSpesialisId
           """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<LayananDto>(sql);
        return result;
    }

    public IEnumerable<LayananDto> ListData(IInstalasiDkKey filter)
    {
        const string sql = """
           SELECT 
               aa.fs_kd_layanan, aa.fs_nm_layanan, aa.fb_aktif,
               aa.fs_kd_instalasi, aa.fs_kd_layanan_dk,
               aa.fs_kd_layanan_tipe_dk, aa.GroupSpesialisId,
               ISNULL(bb.fs_nm_instalasi,'') AS fs_nm_instalasi,
               ISNULL(bb.fs_kd_instalasi_dk, '') AS fs_kd_instalasi_dk,
               ISNULL(cc.fs_nm_layanan_dk,'') AS fs_nm_layanan_dk,
               ISNULL(dd.fs_nm_layanan_tipe_dk,'') AS fs_nm_layanan_tipe_dk,
               ISNULL(ee.fs_nm_instalasi_dk, '') AS fs_nm_instalasi_dk,
               ISNULL(ff.GroupSpesialisName, '') AS GroupSpesialisName
           FROM 
               ta_layanan aa
               LEFT JOIN ta_instalasi bb ON aa.fs_kd_instalasi = bb.fs_kd_instalasi
               LEFT JOIN ta_layanan_dk cc ON aa.fs_kd_layanan_dk = cc.fs_kd_layanan_dk
               LEFT JOIN ta_layanan_tipe_dk dd ON aa.fs_kd_layanan_tipe_dk = dd.fs_kd_layanan_tipe_dk
               LEFT JOIN ta_instalasi_dk ee ON bb.fs_kd_instalasi_dk = ee.fs_kd_instalasi_dk
               LEFT JOIN BILRG_GroupSpesialis ff ON aa.GroupSpesialisId = ff.GroupSpesialisId
           WHERE
               bb.fs_kd_instalasi_dk = @fs_kd_instalasi_dk    
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_instalasi_dk", filter.InstalasiDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<LayananDto>(sql, dp);
        return result;
    }
}



