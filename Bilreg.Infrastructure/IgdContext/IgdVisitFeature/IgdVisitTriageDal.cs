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
                IgdVisitId, NoTriage, TriageLevel, AssessmentDateTime, AssessorUserId, Notes)
            VALUES (
                @IgdVisitId, @NoTriage, @TriageLevel, @AssessmentDateTime, @AssessorUserId, @Notes)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", dto.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@NoTriage", dto.NoTriage, SqlDbType.Int);
        dp.AddParam("@TriageLevel", dto.TriageLevel, SqlDbType.VarChar);
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
            SELECT IgdVisitId, NoTriage, TriageLevel, AssessmentDateTime, AssessorUserId, Notes
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
