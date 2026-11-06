using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabMasterFeature;

public class LabTestGroupDal : ILabTestGroupDal
{
    private const string SelectColumns = """
        TestGroupId,
        TestGroupName,
        CrtUser,
        CrtDate,
        UpdUser,
        UpdDate,
        VodUser,
        VodDate
        """;

    private readonly DatabaseOptions _opt;

    public LabTestGroupDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LabTestGroupDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_LabTestGroup (
                TestGroupId, TestGroupName,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @TestGroupId, @TestGroupName,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(LabTestGroupDto dto)
    {
        const string sql = """
            UPDATE BILRG_LabTestGroup
            SET TestGroupName = @TestGroupName,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE TestGroupId = @TestGroupId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Delete(ILabTestGroupKey key)
    {
        const string sql = """
            DELETE FROM BILRG_LabTestGroup
            WHERE TestGroupId = @TestGroupId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TestGroupId", key.TestGroupId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public LabTestGroupDto? GetData(ILabTestGroupKey key)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabTestGroup
            WHERE TestGroupId = @TestGroupId
              AND VodUser = ''
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TestGroupId", key.TestGroupId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<LabTestGroupDto>(sql, dp);
    }

    public IEnumerable<LabTestGroupDto> ListData()
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabTestGroup
            WHERE VodUser = ''
            ORDER BY TestGroupName
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabTestGroupDto>(sql) ?? Enumerable.Empty<LabTestGroupDto>();
    }

    public IEnumerable<LabTestGroupDto> ListData(string? search)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabTestGroup
            WHERE VodUser = ''
              AND (
                  @Search IS NULL OR @Search = ''
                  OR TestGroupId LIKE @SearchPattern
                  OR TestGroupName LIKE @SearchPattern
              )
            ORDER BY TestGroupName
            """;

        var searchPattern = string.IsNullOrWhiteSpace(search)
            ? null
            : $"%{search.Trim()}%";

        var dp = new DynamicParameters();
        dp.AddParam("@Search", search ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@SearchPattern", searchPattern ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabTestGroupDto>(sql, dp) ?? Enumerable.Empty<LabTestGroupDto>();
    }

    private static DynamicParameters BuildParams(LabTestGroupDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@TestGroupId", dto.TestGroupId, SqlDbType.VarChar);
        dp.AddParam("@TestGroupName", dto.TestGroupName, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
