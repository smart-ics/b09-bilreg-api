using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabComponentMasterFeature;

public interface ILabComponentMasterDal :
    IGetData<LabComponentMasterDto, ILabComponentMasterKey>,
    IListData<LabComponentMasterDto>,
    IListData<LabComponentMasterDto, LabComponentMasterListFilter>
{
    LabComponentMasterDto? GetByComponentCode(string componentCode);
}

public class LabComponentMasterDal : ILabComponentMasterDal
{
    private const string SelectColumns = """
        ComponentId,
        LoincCode,
        ComponentCode,
        ComponentName,
        ComponentNameIndonesia,
        ResultType,
        DefaultUnit,
        IsSystem,
        IsActive,
        CrtUser,
        CrtDate,
        UpdUser,
        UpdDate,
        VodUser,
        VodDate
        """;

    private readonly DatabaseOptions _opt;

    public LabComponentMasterDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public LabComponentMasterDto GetData(ILabComponentMasterKey key)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabComponentMaster
            WHERE ComponentId = @ComponentId
              AND VodUser = ''
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ComponentId", key.ComponentId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<LabComponentMasterDto>(sql, dp);
    }

    public LabComponentMasterDto? GetByComponentCode(string componentCode)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabComponentMaster
            WHERE ComponentCode = @ComponentCode
              AND VodUser = ''
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ComponentCode", componentCode, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<LabComponentMasterDto>(sql, dp);
    }

    public IEnumerable<LabComponentMasterDto> ListData()
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabComponentMaster
            WHERE VodUser = ''
            ORDER BY ComponentCode
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabComponentMasterDto>(sql);
    }

    public IEnumerable<LabComponentMasterDto> ListData(LabComponentMasterListFilter filter)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM BILRG_LabComponentMaster
            WHERE VodUser = ''
              AND (@ActiveOnly = 0 OR IsActive = 1)
              AND (
                  @Search IS NULL
                  OR @Search = ''
                  OR ComponentCode LIKE @SearchPattern
                  OR ComponentName LIKE @SearchPattern
              )
            ORDER BY ComponentCode
            """;

        var searchPattern = string.IsNullOrWhiteSpace(filter.Search)
            ? null
            : $"%{filter.Search.Trim()}%";

        var dp = new DynamicParameters();
        dp.AddParam("@ActiveOnly", filter.ActiveOnly ? 1 : 0, SqlDbType.Bit);
        dp.AddParam("@Search", filter.Search ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@SearchPattern", searchPattern ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabComponentMasterDto>(sql, dp);
    }
}
