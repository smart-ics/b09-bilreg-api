using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.RoomRateFeature;

public interface ITipeKamarDal :
    IInsert<TipeKamarDto>,
    IUpdate<TipeKamarDto>,
    IDelete<ITipeKamarKey>,
    IGetData<TipeKamarDto, ITipeKamarKey>,
    IListData<TipeKamarDto>
{
}

public class TipeKamarDal : ITipeKamarDal
{
    private readonly DatabaseOptions _opt;

    public TipeKamarDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(TipeKamarDto dto)
    {
        const string sql = """
           INSERT INTO ta_kamar_tipe(
               fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
           VALUES( 
               @fs_kd_kamar_tipe, @fs_nm_kamar_tipe, @fb_gabung, @fb_aktif, @fb_default_tipe)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar_tipe", dto.fs_kd_kamar_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kamar_tipe", dto.fs_nm_kamar_tipe, SqlDbType.VarChar);
        dp.AddParam("@fb_gabung", dto.fb_gabung, SqlDbType.Bit);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fb_default_tipe", dto.fb_default_tipe, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeKamarDto dto)
    {
        const string sql = """
           UPDATE 
               ta_kamar_tipe
           SET
               fs_nm_kamar_tipe = @fs_nm_kamar_tipe,
               fb_gabung = @fb_gabung,
               fb_aktif = @fb_aktif,
               fb_default_tipe = @fb_default_tipe
           WHERE
               fs_kd_kamar_tipe = @fs_kd_kamar_tipe
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar_tipe", dto.fs_kd_kamar_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kamar_tipe", dto.fs_nm_kamar_tipe, SqlDbType.VarChar);
        dp.AddParam("@fb_gabung", dto.fb_gabung, SqlDbType.Bit);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fb_default_tipe", dto.fb_default_tipe, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITipeKamarKey key)
    {
        const string sql = """
           DELETE FROM 
               ta_kamar_tipe
           WHERE
               fs_kd_kamar_tipe = @fs_kd_kamar_tipe
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar_tipe", key.TipeKamarId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeKamarDto GetData(ITipeKamarKey key)
    {
        const string sql = """
           SELECT
               fs_kd_kamar_tipe,
               fs_nm_kamar_tipe,
               fb_gabung,
               fb_aktif,
               fb_default_tipe
           FROM 
               ta_kamar_tipe
           WHERE
               fs_kd_kamar_tipe = @fs_kd_kamar_tipe
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar_tipe", key.TipeKamarId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<TipeKamarDto>(sql, dp);
        return result;
    }

    public IEnumerable<TipeKamarDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_kamar_tipe,
                fs_nm_kamar_tipe,
                fb_gabung,
                fb_aktif,
                fb_default_tipe
            FROM 
                ta_kamar_tipe
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeKamarDto>(sql);
    }
}