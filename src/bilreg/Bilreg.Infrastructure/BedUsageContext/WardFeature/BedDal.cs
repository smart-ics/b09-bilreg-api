using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public interface IBedDal :
    IInsert<BedDto>,
    IUpdate<BedDto>,
    IDelete<IBedKey>,
    IGetData<BedDto, IBedKey>,
    IListData<BedDto>
{
}

public class BedDal : IBedDal
{
    private readonly DatabaseOptions _opt;

    public BedDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(BedDto dto)
    {
        const string sql = """
           INSERT INTO ta_bed(
               fs_kd_bed, fs_nm_bed, fs_kd_kamar, fb_aktif)
           VALUES( 
               @fs_kd_bed, @fs_nm_bed, @fs_kd_kamar, @fb_aktif)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bed", dto.fs_kd_bed, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_bed", dto.fs_nm_bed, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kamar", dto.fs_kd_kamar, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BedDto dto)
    {
        const string sql = """
           UPDATE 
               ta_bed
           SET
               fs_nm_bed = @fs_nm_bed,
               fs_kd_kamar = @fs_kd_kamar,
               fb_aktif = @fb_aktif
           WHERE
               fs_kd_bed = @fs_kd_bed
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bed", dto.fs_kd_bed, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_bed", dto.fs_nm_bed, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kamar", dto.fs_kd_kamar, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBedKey key)
    {
        const string sql = """
           DELETE FROM 
               ta_bed
           WHERE
               fs_kd_bed = @fs_kd_bed
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bed", key.BedId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BedDto GetData(IBedKey key)
    {
        const string sql = """
           SELECT
               aa.fs_kd_bed, aa.fs_nm_bed, aa.fs_kd_kamar, aa.fb_aktif,
               ISNULL(bb.fs_nm_kamar, '') fs_nm_kamar,
               ISNULL(bb.fs_kd_bangsal, '') fs_kd_bangsal,
               ISNULL(cc.fs_nm_bangsal, '') fs_nm_bangsal
           FROM 
               ta_bed aa
               LEFT JOIN ta_kamar bb ON aa.fs_kd_kamar = bb.fs_kd_kamar
               LEFT JOIN ta_bangsal cc ON bb.fs_kd_bangsal = cc.fs_kd_bangsal
           WHERE
               aa.fs_kd_bed = @fs_kd_bed
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bed", key.BedId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<BedDto>(sql, dp);
        return result;
    }

    public IEnumerable<BedDto> ListData()
    {
        const string sql = """
           SELECT
               aa.fs_kd_bed, aa.fs_nm_bed, aa.fs_kd_kamar, aa.fb_aktif,
               ISNULL(bb.fs_nm_kamar, '') fs_nm_kamar,
               ISNULL(bb.fs_kd_bangsal, '') fs_kd_bangsal,
               ISNULL(cc.fs_nm_bangsal, '') fs_nm_bangsal
           FROM 
               ta_bed aa
               LEFT JOIN ta_kamar bb ON aa.fs_kd_kamar = bb.fs_kd_kamar
               LEFT JOIN ta_bangsal cc ON bb.fs_kd_bangsal = cc.fs_kd_bangsal
           """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BedDto>(sql);
    }
}