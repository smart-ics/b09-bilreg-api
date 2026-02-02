using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AccountingContext.UnitFeature;

public interface IMapJaminanJkDal :
    IGetData<MapJaminanJkDto, IJaminanKey>,
    IListData<MapJaminanJkDto>
{ }
public class MapJaminanJkDal : IMapJaminanJkDal
{
    private readonly DatabaseOptions _opt;
    public MapJaminanJkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public MapJaminanJkDto GetData(IJaminanKey key)
    {
        const string sql = @"
                SELECT
                    fs_kd_jaminan, fs_kd_jk
                 FROM 
                    ta_map_jmn_jk
                 WHERE
                    fs_kd_jaminan = @fs_kd_jaminan
                 ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", key.JaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<MapJaminanJkDto>(sql, dp);
    }

    public IEnumerable<MapJaminanJkDto> ListData()
    {
        const string sql = @"
                SELECT
                    fs_kd_jaminan, fs_kd_jk
                 FROM 
                    ta_map_jmn_jk
                 ";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<MapJaminanJkDto>(sql);
    }
}

public record MapJaminanJkDto(
    string fs_kd_jaminan,
    string fs_kd_jk)
{
    public MapJaminanJkType ToModel()
    {
        return new MapJaminanJkType(
            fs_kd_jaminan,
            fs_kd_jk);
    }
}