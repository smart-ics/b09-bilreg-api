using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IDischargeOpDal :
    IInsert<DischargeOpDto>,
    IUpdate<DischargeOpDto>,
    IDelete<IDischargeOpKey>,
    IGetData<DischargeOpDto, IDischargeOpKey>,
    IListData<DischargeOpDto, DateTime>
{
}

public class DischargeOpDal : IDischargeOpDal
{
    private readonly DatabaseOptions _opt;

    public DischargeOpDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(DischargeOpDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_DischargeOp(
                DischargeOpId, DischargeOpDate, OrderOpId,
                PasienId, RegId, KamarId, PpaId, PatientCondition, PostOpNote,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate )
            VALUES (
                @DischargeOpId, @DischargeOpDate, @OrderOpId,
                @PasienId, @RegId, @KamarId, @PpaId, @PatientCondition, @PostOpNote,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DischargeOpId", dto.DischargeOpId, SqlDbType.VarChar);
        dp.AddParam("@DischargeOpDate", dto.DischargeOpDate, SqlDbType.DateTime);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@KamarId", dto.KamarId, SqlDbType.VarChar);
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@PatientCondition", dto.PatientCondition, SqlDbType.Int);
        dp.AddParam("@PostOpNote", dto.PostOpNote, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(DischargeOpDto dto)
    {
        const string sql = """
            UPDATE
                BILRG_DischargeOp
            SET
                DischargeOpDate = @DischargeOpDate,
                OrderOpId = @OrderOpId,
                PasienId = @PasienId,
                RegId = @RegId,
                KamarId = @KamarId,
                PpaId = @PpaId,
                PatientCondition = @PatientCondition,
                PostOpNote = @PostOpNote,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                DischargeOpId = @DischargeOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DischargeOpId", dto.DischargeOpId, SqlDbType.VarChar);
        dp.AddParam("@DischargeOpDate", dto.DischargeOpDate, SqlDbType.DateTime);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@KamarId", dto.KamarId, SqlDbType.VarChar);
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@PatientCondition", dto.PatientCondition, SqlDbType.Int);
        dp.AddParam("@PostOpNote", dto.PostOpNote, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IDischargeOpKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_DischargeOp
            WHERE
                DischargeOpId = @DischargeOpId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@DischargeOpId", key.DischargeOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public DischargeOpDto GetData(IDischargeOpKey key)
    {
        const string sql = """
            SELECT
                DischargeOpId, DischargeOpDate, OrderOpId,
                PasienId, RegId, KamarId, PpaId, PatientCondition, PostOpNote,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender,
                ISNULL(cc.fs_nm_kamar, '') AS KamarName,
                ISNULL(dd.fs_nm_peg, '') AS PpaName
            FROM
                BILRG_DischargeOp aa
                LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
                LEFT JOIN ta_kamar cc ON aa.KamarId = cc.fs_kd_kamar
                LEFT JOIN td_peg dd ON aa.PpaId = dd.fs_kd_peg
            WHERE
                DischargeOpId = @DischargeOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DischargeOpId", key.DischargeOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<DischargeOpDto>(sql, dp);
        return result;
    }


    public IEnumerable<DischargeOpDto> ListData(DateTime filter)
    {
        const string sql = """
            SELECT
                DischargeOpId, DischargeOpDate, OrderOpId,
                PasienId, RegId, KamarId, PpaId, PatientCondition, PostOpNote,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender,
                ISNULL(cc.fs_nm_kamar, '') AS KamarName,
                ISNULL(dd.fs_nm_peg, '') AS PpaName
            FROM
                BILRG_DischargeOp aa
                LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
                LEFT JOIN ta_kamar cc ON aa.KamarId = cc.fs_kd_kamar
                LEFT JOIN td_peg dd ON aa.PpaId = dd.fs_kd_peg
            WHERE
                aa.DischargeOpDate BETWEEN @StartDate AND @EndDate
            """;

        var tgl1 = filter.Date;
        var tgl2 = filter.Date.AddHours(23.0).AddMinutes(59.0).AddSeconds(59.0);
        var dp = new DynamicParameters();
        dp.AddParam("@StartDate", tgl1, SqlDbType.DateTime);
        dp.AddParam("@EndDate", tgl2, SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<DischargeOpDto>(sql, dp);
    }
}