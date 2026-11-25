using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IPreOpClearanceChecklistDal
{
    void Insert(IEnumerable<PreOpClearanceChecklistDto> listDto);
    void Delete(string clearanceId);
    IEnumerable<PreOpClearanceChecklistDto> ListData(string clearanceId);
}

public class PreOpClearanceChecklistDal : IPreOpClearanceChecklistDal
{
    private readonly DatabaseOptions _opt;

    public PreOpClearanceChecklistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PreOpClearanceChecklistDto> listDto)
    {
        var fetched = listDto.ToList();
        if (fetched.Count == 0)
            return;

        const string sql = """
            INSERT INTO BILRG_PreOpClearanceChecklist (
                ClearanceId, NoUrut, ChecklistKey, Status, Notes
            )
            VALUES (
                @ClearanceId, @NoUrut, @ChecklistKey, @Status, @Notes
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();

        foreach (var dto in fetched)
        {
            var dp = new DynamicParameters();
            dp.AddParam("@ClearanceId", dto.ClearanceId, SqlDbType.VarChar);
            dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
            dp.AddParam("@ChecklistKey", dto.ChecklistKey, SqlDbType.VarChar);
            dp.AddParam("@Status", dto.Status, SqlDbType.Int);
            dp.AddParam("@Notes", dto.Notes, SqlDbType.VarChar);
            conn.Execute(sql, dp);
        }
    }

    public void Delete(string clearanceId)
    {
        const string sql = """
            DELETE FROM
                BILRG_PreOpClearanceChecklist
            WHERE
                ClearanceId = @ClearanceId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ClearanceId", clearanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PreOpClearanceChecklistDto> ListData(string clearanceId)
    {
        const string sql = """
            SELECT
                aa.*
            FROM
                BILRG_PreOpClearanceChecklist aa
            WHERE
                aa.ClearanceId = @ClearanceId
            ORDER BY
                aa.NoUrut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ClearanceId", clearanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PreOpClearanceChecklistDto>(sql, dp) ?? [];
    }
}
