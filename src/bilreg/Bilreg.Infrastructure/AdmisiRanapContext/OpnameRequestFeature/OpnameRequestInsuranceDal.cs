
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiRanapContext.OpnameRequestFeature;


public interface IOpnameRequestInsuranceDal :
    IInsert<OpnameRequestInsuranceDto>,
    IUpdate<OpnameRequestInsuranceDto>,
    IGetData<OpnameRequestInsuranceDto, IOpnameRequestKey>
{ }
public class OpnameRequestInsuranceDal : IOpnameRequestInsuranceDal
{
    private const string SELECT_COLUMNS = """
        aa.OpnameRequestId, aa.TipeJaminanId,
        aa.TipeJaminanName, aa.ReffId
        """;
    private readonly DatabaseOptions _opt;

    public OpnameRequestInsuranceDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(OpnameRequestInsuranceDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AdmOpnameRequestInsurance (
                OpnameRequestId, TipeJaminanId, TipeJaminanName, ReffId)
            VALUES (
                @OpnameRequestId, @TipeJaminanId, @TipeJaminanName, @ReffId)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public void Update(OpnameRequestInsuranceDto dto)
    {
        const string sql = """
            UPDATE BILRG_AdmOpnameRequest
            SET
                OpnameRequestStatus = @OpnameRequestStatus,
                PasienId = @PasienId,
                DokterId = @DokterId,
                DokterName = @DokterName,
                PlannedDate = @PlannedDate,
                ClinicalNotes = @ClinicalNotes,
                FulfilledRegId = @FulfilledRegId,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                OpnameRequestId = @OpnameRequestId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapWriteParams(dto));
    }

    public OpnameRequestInsuranceDto GetData(IOpnameRequestKey key)
    {
        const string sql = $"""
                            SELECT
                                {SELECT_COLUMNS}
                            FROM BILRG_AdmOpnameRequestInsurance aa
                            WHERE aa.OpnameRequestId = @OpnameRequestId
                            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OpnameRequestId", key.OpnameRequestId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<OpnameRequestInsuranceDto>(sql, dp);
    }

    private static DynamicParameters MapWriteParams(OpnameRequestInsuranceDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@OpnameRequestId", dto.OpnameRequestId, SqlDbType.VarChar);
        dp.AddParam("@TipeJaminanId", dto.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@TipeJaminanName", dto.TipeJaminanName, SqlDbType.VarChar);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        return dp;
    }
}
