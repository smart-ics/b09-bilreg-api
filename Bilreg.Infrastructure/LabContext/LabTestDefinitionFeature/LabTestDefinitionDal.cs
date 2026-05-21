using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabTestDefinitionFeature;

public interface ILabTestDefinitionDal :
    IInsert<LabTestDefinitionDto>,
    IUpdate<LabTestDefinitionDto>,
    IDelete<ILabTestDefinitionKey>,
    IGetData<LabTestDefinitionDto?, ILabTestDefinitionKey>,
    IListData<LabTestDefinitionDto>,
    IListData<LabTestDefinitionDto, LabTestDefinitionListFilter>
{
    string? GetMaxTestDefinitionId();
    LabTestDefinitionDto? GetActiveByTarifId(string tarifId, string? excludeTestDefinitionId);
}

public class LabTestDefinitionDal : ILabTestDefinitionDal
{
    private const string SelectColumns = """
        TestDefinitionId,
        TarifId,
        TarifCode,
        TarifName,
        LabTestCode,
        LabTestName,
        SpecimenType,
        VacutainerType,
        IsActive,
        CrtUser,
        CrtDate,
        UpdUser,
        UpdDate,
        VodUser,
        VodDate
        """;

    private readonly DatabaseOptions _opt;

    public LabTestDefinitionDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LabTestDefinitionDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_LabTestDefinition (
                TestDefinitionId, TarifId, TarifCode, TarifName,
                LabTestCode, LabTestName, SpecimenType, VacutainerType, IsActive,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @TestDefinitionId, @TarifId, @TarifCode, @TarifName,
                @LabTestCode, @LabTestName, @SpecimenType, @VacutainerType, @IsActive,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(LabTestDefinitionDto dto)
    {
        const string sql = """
            UPDATE BILRG_LabTestDefinition
            SET TarifId = @TarifId,
                TarifCode = @TarifCode,
                TarifName = @TarifName,
                LabTestCode = @LabTestCode,
                LabTestName = @LabTestName,
                SpecimenType = @SpecimenType,
                VacutainerType = @VacutainerType,
                IsActive = @IsActive,
                CrtUser = @CrtUser, CrtDate = @CrtDate,
                UpdUser = @UpdUser, UpdDate = @UpdDate,
                VodUser = @VodUser, VodDate = @VodDate
            WHERE TestDefinitionId = @TestDefinitionId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Delete(ILabTestDefinitionKey key)
    {
        const string sql = """
            DELETE FROM BILRG_LabTestDefinition
            WHERE TestDefinitionId = @TestDefinitionId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TestDefinitionId", key.TestDefinitionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public LabTestDefinitionDto? GetData(ILabTestDefinitionKey key)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabTestDefinition
            WHERE TestDefinitionId = @TestDefinitionId
              AND VodUser = ''
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TestDefinitionId", key.TestDefinitionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<LabTestDefinitionDto>(sql, dp);
    }

    public IEnumerable<LabTestDefinitionDto> ListData()
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabTestDefinition
            WHERE VodUser = ''
            ORDER BY LabTestCode
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabTestDefinitionDto>(sql);
    }

    public IEnumerable<LabTestDefinitionDto> ListData(LabTestDefinitionListFilter filter)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabTestDefinition
            WHERE VodUser = ''
              AND (@ActiveOnly = 0 OR IsActive = 1)
              AND (
                  @TarifId IS NULL OR @TarifId = '' OR TarifId = @TarifId
              )
              AND (
                  @Search IS NULL OR @Search = ''
                  OR LabTestCode LIKE @SearchPattern
                  OR LabTestName LIKE @SearchPattern
                  OR TarifCode LIKE @SearchPattern
                  OR TarifId LIKE @SearchPattern
              )
            ORDER BY LabTestCode
            """;

        var searchPattern = string.IsNullOrWhiteSpace(filter.Search)
            ? null
            : $"%{filter.Search.Trim()}%";

        var dp = new DynamicParameters();
        dp.AddParam("@ActiveOnly", filter.ActiveOnly ? 1 : 0, SqlDbType.Bit);
        dp.AddParam("@TarifId", filter.TarifId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@Search", filter.Search ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@SearchPattern", searchPattern ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabTestDefinitionDto>(sql, dp);
    }

    public string? GetMaxTestDefinitionId()
    {
        const string sql = """
            SELECT MAX(TestDefinitionId) AS MaxId
            FROM BILRG_LabTestDefinition
            WHERE LEFT(TestDefinitionId, 3) = 'LTD'
              AND LEN(TestDefinitionId) = 7
              AND VodUser = ''
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<string?>(sql);
    }

    public LabTestDefinitionDto? GetActiveByTarifId(string tarifId, string? excludeTestDefinitionId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabTestDefinition
            WHERE TarifId = @TarifId
              AND IsActive = 1
              AND VodUser = ''
              AND (@ExcludeId IS NULL OR @ExcludeId = '' OR TestDefinitionId <> @ExcludeId)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TarifId", tarifId, SqlDbType.VarChar);
        dp.AddParam("@ExcludeId", excludeTestDefinitionId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<LabTestDefinitionDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(LabTestDefinitionDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@TestDefinitionId", dto.TestDefinitionId, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifCode", dto.TarifCode, SqlDbType.VarChar);
        dp.AddParam("@TarifName", dto.TarifName, SqlDbType.VarChar);
        dp.AddParam("@LabTestCode", dto.LabTestCode, SqlDbType.VarChar);
        dp.AddParam("@LabTestName", dto.LabTestName, SqlDbType.VarChar);
        dp.AddParam("@SpecimenType", dto.SpecimenType, SqlDbType.VarChar);
        dp.AddParam("@VacutainerType", dto.VacutainerType, SqlDbType.Int);
        dp.AddParam("@IsActive", dto.IsActive, SqlDbType.Bit);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
