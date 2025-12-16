using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IStartOpDal :
    IInsert<StartOpDto>,
    IUpdate<StartOpDto>,
    IDelete<IStartOpKey>,
    IGetData<StartOpDto, IStartOpKey>,
    IListData<StartOpDto, DateTime>
{
}

public class StartOpDal : IStartOpDal
{
    private readonly DatabaseOptions _opt;

    public StartOpDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(StartOpDto dto)
    {
        const string sql = @"
            INSERT INTO BILRG_StartOp (
                StartOpId, StartOpTime, OrderOpId, ScheduleOpId,
                RegId, PasienId, KamarOpId, PpaId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            )
            VALUES (
                @StartOpId, @StartOpTime, @OrderOpId, @ScheduleOpId,
                @RegId, @PasienId, @KamarOpId, @PpaId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate
            )";

        var dp = new DynamicParameters();
        dp.AddParam("@StartOpId", dto.StartOpId, SqlDbType.VarChar);
        dp.AddParam("@StartOpTime", dto.StartOpTime, SqlDbType.DateTime);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@KamarOpId", dto.KamarOpId, SqlDbType.VarChar);
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(StartOpDto dto)
    {
        const string sql = @"
            UPDATE
                BILRG_StartOp
            SET
                StartOpTime = @StartOpTime,
                OrderOpId = @OrderOpId,
                ScheduleOpId = @ScheduleOpId,
                RegId = @RegId,
                PasienId = @PasienId,
                KamarOpId = @KamarOpId,
                PpaId = @PpaId,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE StartOpId = @StartOpId";

        var dp = new DynamicParameters();
        dp.AddParam("@StartOpId", dto.StartOpId, SqlDbType.VarChar);
        dp.AddParam("@StartOpTime", dto.StartOpTime, SqlDbType.DateTime);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@KamarOpId", dto.KamarOpId, SqlDbType.VarChar);
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IStartOpKey key)
    {
        const string sql = @"
            DELETE FROM
                BILRG_StartOp
            WHERE StartOpId = @StartOpId";

        var dp = new DynamicParameters();
        dp.AddParam("@StartOpId", key.StartOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public StartOpDto GetData(IStartOpKey key)
    {
        const string sql = @"
            SELECT
                aa.StartOpId, aa.StartOpTime, aa.OrderOpId, aa.ScheduleOpId,
                aa.RegId, aa.PasienId, aa.KamarOpId, aa.PpaId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
                ISNULL(bb.NamaOperasi, '') AS NamaOperasi,
                ISNULL(cc.fs_nm_pasien, '') AS PasienName,
                ISNULL(cc.fd_tgl_lahir, '3000-01-01') AS TglLahir,
                ISNULL(cc.fs_jns_kelamin, '') AS Gender,
                ISNULL(dd.fs_nm_kamar, '') AS KamarName,
                ISNULL(ee.fs_nm_peg, '') AS PpaName
            FROM
                BILRG_StartOp aa
                LEFT JOIN BILRG_OrderOp bb ON aa.OrderOpId = bb.OrderOpId
                LEFT JOIN tc_mr cc ON aa.PasienId = cc.fs_mr
                LEFT JOIN ta_kamar dd ON aa.KamarId = dd.fs_kd_kamar
                LEFT JOIN td_peg ee ON aa.PpaId = ee.fs_kd_peg
            WHERE
                aa.StartOpId = @StartOpId";

        var dp = new DynamicParameters();
        dp.AddParam("@StartOpId", key.StartOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<StartOpDto>(sql, dp);
        return result;
    }

    public IEnumerable<StartOpDto> ListData(DateTime filter)
    {
        const string sql = @"
            SELECT
                aa.StartOpId, aa.StartOpTime, aa.OrderOpId, aa.ScheduleOpId,
                aa.RegId, aa.PasienId, aa.KamarOpId, aa.PpaId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
                ISNULL(bb.NamaOperasi, '') AS NamaOperasi,
                ISNULL(cc.fs_nm_pasien, '') AS PasienName,
                ISNULL(cc.fd_tgl_lahir, '3000-01-01') AS TglLahir,
                ISNULL(cc.fs_jns_kelamin, '') AS Gender,
                ISNULL(dd.fs_nm_kamar, '') AS KamarName,
                ISNULL(ee.fs_nm_peg, '') AS PpaName
            FROM
                BILRG_StartOp aa
                LEFT JOIN BILRG_OrderOp bb ON aa.OrderOpId = bb.OrderOpId
                LEFT JOIN tc_mr cc ON aa.PasienId = cc.fs_mr
                LEFT JOIN ta_kamar dd ON aa.KamarId = dd.fs_kd_kamar
                LEFT JOIN td_peg ee ON aa.PpaId = ee.fs_kd_peg
            WHERE
                StartOpTime BETWEEN @Tgl1 AND @Tgl2";

        var tgl1 = filter.Date;
        var tgl2 = filter.Date.AddHours(23.0).AddMinutes(59.0).AddSeconds(59.0);
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<StartOpDto>(sql, dp);
        return result;
    }
}