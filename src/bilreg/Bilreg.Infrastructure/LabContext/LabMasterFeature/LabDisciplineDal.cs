using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabMasterFeature;

public class LabDisciplineDal : ILabDisciplineDal
{
    private const string SelectColumns = """
        DisciplineId,
        DisciplineName,
        CrtUser,
        CrtDate,
        UpdUser,
        UpdDate,
        VodUser,
        VodDate
        """;

    private readonly DatabaseOptions _opt;

    public LabDisciplineDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LabDisciplineDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_LabDiscipline (
                DisciplineId, DisciplineName,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @DisciplineId, @DisciplineName,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(LabDisciplineDto dto)
    {
        const string sql = """
            UPDATE BILRG_LabDiscipline
            SET DisciplineName = @DisciplineName,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE DisciplineId = @DisciplineId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Delete(ILabDisciplineKey key)
    {
        const string sql = """
            DELETE FROM BILRG_LabDiscipline
            WHERE DisciplineId = @DisciplineId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DisciplineId", key.DisciplineId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public LabDisciplineDto? GetData(ILabDisciplineKey key)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabDiscipline
            WHERE DisciplineId = @DisciplineId
              AND VodUser = ''
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DisciplineId", key.DisciplineId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<LabDisciplineDto>(sql, dp);
    }

    public IEnumerable<LabDisciplineDto> ListData()
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabDiscipline
            WHERE VodUser = ''
            ORDER BY DisciplineName
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabDisciplineDto>(sql) ?? Enumerable.Empty<LabDisciplineDto>();
    }

    public IEnumerable<LabDisciplineDto> ListData(string? search)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabDiscipline
            WHERE VodUser = ''
              AND (
                  @Search IS NULL OR @Search = ''
                  OR DisciplineId LIKE @SearchPattern
                  OR DisciplineName LIKE @SearchPattern
              )
            ORDER BY DisciplineName
            """;

        var searchPattern = string.IsNullOrWhiteSpace(search)
            ? null
            : $"%{search.Trim()}%";

        var dp = new DynamicParameters();
        dp.AddParam("@Search", search ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@SearchPattern", searchPattern ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabDisciplineDto>(sql, dp) ?? Enumerable.Empty<LabDisciplineDto>();
    }

    private static DynamicParameters BuildParams(LabDisciplineDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@DisciplineId", dto.DisciplineId, SqlDbType.VarChar);
        dp.AddParam("@DisciplineName", dto.DisciplineName, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
