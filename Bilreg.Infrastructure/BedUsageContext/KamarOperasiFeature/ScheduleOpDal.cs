using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IScheduleOpDal :
    IInsert<ScheduleOpDto>,
    IUpdate<ScheduleOpDto>,
    IDelete<IScheduleOpKey>,
    IGetData<ScheduleOpDto, IScheduleOpKey>,
    IListData<ScheduleOpDto, DateTime>,
    IListData<ScheduleOpDto, IPasienKey>
{
}

public class ScheduleOpDal : IScheduleOpDal
{
    private readonly DatabaseOptions _opt;

    public ScheduleOpDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(ScheduleOpDto dto)
    {
        const string sql = """
           INSERT INTO BILRG_ScheduleOp(
               ScheduleOpId, ScheduleOpDate, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate,
               OrderOpId, PasienId, UrgencyLevel, Durasi, TglOp, KamarId, RegId, PpaId)
           VALUES( 
               @ScheduleOpId, @ScheduleOpDate, @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate,
               @OrderOpId, @PasienId, @UrgencyLevel, @Durasi, @TglOp, @KamarId, @RegId, @PpaId)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpDate", dto.ScheduleOpDate, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@UrgencyLevel", dto.UrgencyLevel, SqlDbType.Int);
        dp.AddParam("@Durasi", dto.Durasi, SqlDbType.Int);
        dp.AddParam("@TglOp", dto.TglOp, SqlDbType.DateTime);
        dp.AddParam("@KamarId", dto.KamarId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(ScheduleOpDto dto)
    {
        const string sql = """
           UPDATE 
               BILRG_ScheduleOp
           SET
               ScheduleOpDate = @ScheduleOpDate,
               UpdUser = @UpdUser,
               UpdDate = @UpdDate,
               OrderOpId = @OrderOpId,
               PasienId = @PasienId,
               UrgencyLevel = @UrgencyLevel,
               Durasi = @Durasi,
               TglOp = @TglOp,
               KamarId = @KamarId,
               RegId = @RegId,
               PpaId = @PpaId,
               VodUser = @VodUser,
               VodDate = @VodDate
           WHERE
               ScheduleOpId = @ScheduleOpId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpDate", dto.ScheduleOpDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@UrgencyLevel", dto.UrgencyLevel, SqlDbType.Int);
        dp.AddParam("@Durasi", dto.Durasi, SqlDbType.Int);
        dp.AddParam("@TglOp", dto.TglOp, SqlDbType.DateTime);
        dp.AddParam("@KamarId", dto.KamarId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IScheduleOpKey key)
    {
        const string sql = """
           DELETE FROM 
                BILRG_ScheduleOp
           WHERE
               ScheduleOpId = @ScheduleOpId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", key.ScheduleOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public ScheduleOpDto GetData(IScheduleOpKey key)
    {
        const string sql = """
           SELECT
               aa.ScheduleOpId, aa.ScheduleOpDate, aa.OrderOpId, aa.PasienId, aa.UrgencyLevel,
               aa.Durasi, aa.TglOp, aa.KamarId, aa.RegId, aa.PpaId,
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
               ISNULL(bb.OrderDate, '3000-01-01') AS OrderDate,
               ISNULL(bb.NamaOperasi, '') AS NamaOperasi,
               ISNULL(cc.fs_nm_pasien, '') AS PasienName,
               ISNULL(cc.fd_tgl_lahir, '3000-01-01') AS TglLahir,
               ISNULL(cc.fs_jns_kelamin, '') AS Gender,
               ISNULL(dd.fs_nm_kamar, '') AS KamarName,
               ISNULL(ee.fs_nm_peg, '') AS PpaName,
               ISNULL(ff.StartedDate, '3000-01-01') AS StartedDate,
               ISNULL(ff.FinishedDate, '3000-01-01') AS FinishedDate,
               ISNULL(ff.OrderOpState, 0) AS OrderOpState
           FROM
               BILRG_ScheduleOp aa
               LEFT JOIN BILRG_OrderOp bb ON aa.OrderOpId = bb.OrderOpId
               LEFT JOIN tc_mr cc ON aa.PasienId = cc.fs_mr
               LEFT JOIN ta_kamar dd ON aa.KamarId = dd.fs_kd_kamar
               LEFT JOIN td_peg ee ON aa.PpaId = ee.fs_kd_peg
               LEFT JOIN BILRG_OpCase ff ON aa.OrderOpId = ff.OrderOpId
           WHERE
               aa.ScheduleOpId = @ScheduleOpId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", key.ScheduleOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<ScheduleOpDto>(sql, dp);
        return result;
    }

    public IEnumerable<ScheduleOpDto> ListData(DateTime filter)
    {
        const string sql = """
            SELECT
                aa.ScheduleOpId, aa.ScheduleOpDate, aa.OrderOpId, aa.PasienId, aa.UrgencyLevel,
                aa.Durasi, aa.TglOp, aa.KamarId, aa.RegId, aa.PpaId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
                ISNULL(bb.OrderDate, '3000-01-01') AS OrderDate,
                ISNULL(bb.NamaOperasi, '') AS NamaOperasi,
                ISNULL(cc.fs_nm_pasien, '') AS PasienName,
                ISNULL(cc.fd_tgl_lahir, '3000-01-01') AS TglLahir,
                ISNULL(cc.fs_jns_kelamin, '') AS Gender,
                ISNULL(dd.fs_nm_kamar, '') AS KamarName,
                ISNULL(ee.fs_nm_peg, '') AS PpaName,
                ISNULL(ff.StartedDate, '3000-01-01') AS StartedDate,
                ISNULL(ff.FinishedDate, '3000-01-01') AS FinishedDate,
                ISNULL(ff.OrderOpState, 0) AS OrderOpState
            FROM
                BILRG_ScheduleOp aa
                LEFT JOIN BILRG_OrderOp bb ON aa.OrderOpId = bb.OrderOpId
                LEFT JOIN tc_mr cc ON aa.PasienId = cc.fs_mr
                LEFT JOIN ta_kamar dd ON aa.KamarId = dd.fs_kd_kamar
                LEFT JOIN td_peg ee ON aa.PpaId = ee.fs_kd_peg
                LEFT JOIN BILRG_OpCase ff ON aa.OrderOpId = ff.OrderOpId
            WHERE
                aa.TglOp BETWEEN @Tgl1 AND @Tgl2
            """;

        var tgl1 = filter.Date;
        var tgl2 = filter.Date.AddHours(23.0).AddMinutes(59.0).AddSeconds(59.0);
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", tgl2, SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ScheduleOpDto>(sql, dp);
    }

    public IEnumerable<ScheduleOpDto> ListData(IPasienKey filter)
    {
        const string sql = """
            SELECT
                aa.ScheduleOpId, aa.ScheduleOpDate, aa.OrderOpId, aa.PasienId, aa.UrgencyLevel,
                aa.Durasi, aa.TglOp, aa.KamarId, aa.RegId, aa.PpaId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
                ISNULL(bb.OrderDate, '3000-01-01') AS OrderDate,
                ISNULL(bb.NamaOperasi, '') AS NamaOperasi,
                ISNULL(cc.fs_nm_pasien, '') AS PasienName,
                ISNULL(cc.fd_tgl_lahir, '3000-01-01') AS TglLahir,
                ISNULL(cc.fs_jns_kelamin, '') AS Gender,
                ISNULL(dd.fs_nm_kamar, '') AS KamarName,
                ISNULL(ee.fs_nm_peg, '') AS PpaName,
                ISNULL(ff.StartedDate, '3000-01-01') AS StartedDate,
                ISNULL(ff.FinishedDate, '3000-01-01') AS FinishedDate,
                ISNULL(ff.OrderOpState, 0) AS OrderOpState
            FROM
                BILRG_ScheduleOp aa
                LEFT JOIN BILRG_OrderOp bb ON aa.OrderOpId = bb.OrderOpId
                LEFT JOIN tc_mr cc ON aa.PasienId = cc.fs_mr
                LEFT JOIN ta_kamar dd ON aa.KamarId = dd.fs_kd_kamar
                LEFT JOIN td_peg ee ON aa.PpaId = ee.fs_kd_peg
                LEFT JOIN BILRG_OpCase ff ON aa.OrderOpId = ff.OrderOpId
            WHERE
                aa.PasienId = @PasienId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ScheduleOpDto>(sql, dp);
    }
}