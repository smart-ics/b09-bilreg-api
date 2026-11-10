using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public interface IBangsalDal :
    IInsert<BangsalDto>,
    IUpdate<BangsalDto>,
    IDelete<IBangsalKey>,
    IGetData<BangsalDto, IBangsalKey>,
    IListData<BangsalDto>
{
    BangsalDto? GetBangsalByRegId(string regId);
}

public class BangsalDal : IBangsalDal
{
    private readonly DatabaseOptions _opt;

    public BangsalDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(BangsalDto dto)
    {
        const string sql = """
                           INSERT INTO ta_bangsal(
                               fs_kd_bangsal, fs_nm_bangsal, fs_kd_layanan, fs_kd_roomcat)
                           VALUES( 
                               @fs_kd_bangsal, @fs_nm_bangsal, @fs_kd_layanan, @fs_kd_roomcat)
                           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bangsal", dto.fs_kd_bangsal, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_bangsal", dto.fs_nm_bangsal, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_roomcat", dto.fs_kd_roomcat, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BangsalDto dto)
    {
        const string sql = """
           UPDATE 
               ta_bangsal
           SET
               fs_nm_bangsal = @fs_nm_bangsal,
               fs_kd_layanan = @fs_kd_layanan,
               fs_kd_roomcat = @fs_kd_roomcat
           WHERE
               fs_kd_bangsal = @fs_kd_bangsal
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bangsal", dto.fs_kd_bangsal, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_bangsal", dto.fs_nm_bangsal, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_roomcat", dto.fs_kd_roomcat, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBangsalKey key)
    {
        const string sql = """
           DELETE FROM 
               ta_bangsal
           WHERE
               fs_kd_bangsal = @fs_kd_bangsal
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bangsal", key.BangsalId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BangsalDto GetData(IBangsalKey key)
    {
        const string sql = """
           SELECT
               aa.fs_kd_bangsal, aa.fs_nm_bangsal, aa.fs_kd_layanan, aa.fs_kd_roomcat,
               ISNULL(bb.fs_nm_layanan, '') fs_nm_layanan,
               ISNULL(cc.RoomCatName, '') fs_nm_roomcat
           FROM 
               ta_bangsal aa
               LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
               LEFT JOIN BILRG_RoomCat cc ON aa.fs_kd_roomcat = cc.RoomCatId
           WHERE
               aa.fs_kd_bangsal = @fs_kd_bangsal
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bangsal", key.BangsalId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<BangsalDto>(sql, dp);
        return result;
    }

    public IEnumerable<BangsalDto> ListData()
    {
        const string sql = """
           SELECT
               aa.fs_kd_bangsal, aa.fs_nm_bangsal, aa.fs_kd_layanan, aa.fs_kd_roomcat,
               ISNULL(bb.fs_nm_layanan, '') fs_nm_layanan,
               ISNULL(cc.RoomCatName, '') fs_nm_roomcat
           FROM 
               ta_bangsal aa
               LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
               LEFT JOIN BILRG_RoomCat cc ON aa.fs_kd_roomcat = cc.RoomCatId
           WHERE
               ISNULL(aa.fb_aktif, 1) = 1
           """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BangsalDto>(sql);
    }

    public BangsalDto? GetBangsalByRegId(string regId)
    {
        const string sql = """
            SELECT TOP 1
                aa.fs_kd_bangsal, aa.fs_nm_bangsal, aa.fs_kd_layanan, aa.fs_kd_roomcat,
                ISNULL(bb.fs_nm_layanan, '') fs_nm_layanan,
                ISNULL(cc.RoomCatName, '') fs_nm_roomcat
            FROM 
                (
                    SELECT BangsalId FROM BILRG_AdmAdmission WHERE RegId = @regId AND VodDate = '3000-01-01'
                    UNION ALL
                    SELECT BangsalId FROM BILRG_BedWaitingList WHERE RegId = @regId AND VodDate = '3000-01-01'
                    UNION ALL
                    SELECT km.fs_kd_bangsal AS BangsalId 
                    FROM ta_trs_bed pb 
                    INNER JOIN ta_bed bd ON pb.fs_kd_bed = bd.fs_kd_bed
                    INNER JOIN ta_kamar km ON bd.fs_kd_kamar = km.fs_kd_kamar
                    WHERE pb.fs_kd_reg = @regId AND pb.fd_tgl_void = '3000-01-01'
                ) x
                INNER JOIN ta_bangsal aa ON x.BangsalId = aa.fs_kd_bangsal
                LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
                LEFT JOIN BILRG_RoomCat cc ON aa.fs_kd_roomcat = cc.RoomCatId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@regId", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BangsalDto>(sql, dp);
    }
}
