using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitFeature;

public interface IIgdVisitTriageDal :
    IInsert<IgdVisitTriageDto>,
    IDelete<IIgdVisitKey>,
    IListData<IgdVisitTriageDto, IIgdVisitKey>
{
}

public class IgdVisitTriageDal : IIgdVisitTriageDal
{
    private readonly DatabaseOptions _opt;

    public IgdVisitTriageDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IgdVisitTriageDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_IgdVisitTriage (
                IgdVisitId, NoTriage, TriageMethod, TriageLevel, TriageColor,
                AirwaysScore, BreathingScore, BloodCirculationScore,
                GcsEyeScore, GcsMotorScore, GcsVoiceScore,
                IsManualOverrideBlack, OverrideByUserId, OverrideReason, OverrideDateTime,
                AssessmentDateTime, AssessorUserId, Notes)
            VALUES (
                @IgdVisitId, @NoTriage, @TriageMethod, @TriageLevel, @TriageColor,
                @AirwaysScore, @BreathingScore, @BloodCirculationScore,
                @GcsEyeScore, @GcsMotorScore, @GcsVoiceScore,
                @IsManualOverrideBlack, @OverrideByUserId, @OverrideReason, @OverrideDateTime,
                @AssessmentDateTime, @AssessorUserId, @Notes)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", dto.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@NoTriage", dto.NoTriage, SqlDbType.Int);
        dp.AddParam("@TriageMethod", dto.TriageMethod, SqlDbType.VarChar);
        dp.AddParam("@TriageLevel", dto.TriageLevel, SqlDbType.VarChar);
        dp.AddParam("@TriageColor", dto.TriageColor, SqlDbType.VarChar);
        dp.AddParam("@AirwaysScore", dto.AirwaysScore, SqlDbType.Int);
        dp.AddParam("@BreathingScore", dto.BreathingScore, SqlDbType.Int);
        dp.AddParam("@BloodCirculationScore", dto.BloodCirculationScore, SqlDbType.Int);
        dp.AddParam("@GcsEyeScore", dto.GcsEyeScore, SqlDbType.Int);
        dp.AddParam("@GcsMotorScore", dto.GcsMotorScore, SqlDbType.Int);
        dp.AddParam("@GcsVoiceScore", dto.GcsVoiceScore, SqlDbType.Int);
        dp.AddParam("@IsManualOverrideBlack", dto.IsManualOverrideBlack, SqlDbType.Bit);
        dp.AddParam("@OverrideByUserId", dto.OverrideByUserId, SqlDbType.VarChar);
        dp.AddParam("@OverrideReason", dto.OverrideReason, SqlDbType.VarChar);
        dp.AddParam("@OverrideDateTime", dto.OverrideDateTime, SqlDbType.DateTime);
        dp.AddParam("@AssessmentDateTime", dto.AssessmentDateTime, SqlDbType.DateTime);
        dp.AddParam("@AssessorUserId", dto.AssessorUserId, SqlDbType.VarChar);
        dp.AddParam("@Notes", dto.Notes, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IIgdVisitKey key)
    {
        const string sql = """
            DELETE BILRG_IgdVisitTriage WHERE IgdVisitId = @IgdVisitId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", key.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<IgdVisitTriageDto> ListData(IIgdVisitKey key)
    {
        const string sql = """
            SELECT
                IgdVisitId, NoTriage, TriageMethod, TriageLevel, TriageColor,
                AirwaysScore, BreathingScore, BloodCirculationScore,
                GcsEyeScore, GcsMotorScore, GcsVoiceScore,
                IsManualOverrideBlack, OverrideByUserId, OverrideReason, OverrideDateTime,
                AssessmentDateTime, AssessorUserId, Notes
            FROM BILRG_IgdVisitTriage
            WHERE IgdVisitId = @IgdVisitId
            ORDER BY NoTriage
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", key.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<IgdVisitTriageDto>(sql, dp);
    }
}
