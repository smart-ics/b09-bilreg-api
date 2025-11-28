using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Infrastructure.AdmisiContext.LayananFeature.LayananAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.LayananFeature.Spesialis;


public interface IGroupSpesialisDal :
    IGetData<GroupSpesialisType, IGroupSpesialisKey>,
    IListData<GroupSpesialisType>
{ }
public class GroupSpesialisDal : IGroupSpesialisDal
{
    private readonly DatabaseOptions _opt;

    public GroupSpesialisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public GroupSpesialisType GetData(IGroupSpesialisKey key)
    {
        const string sql = """
           SELECT 
               aa.GroupSpesialisId, aa.GroupSpesialisName
           FROM 
               BILRG_GroupSpesialis aa
           WHERE 
               aa.GroupSpesialisId = @GroupSpesialisId     
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@GroupSpesialisId", key.GroupSpesialisId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GroupSpesialisType>(sql, dp);
        return result;
    }

    public IEnumerable<GroupSpesialisType> ListData()
    {
        const string sql = """
           SELECT 
               aa.GroupSpesialisId, aa.GroupSpesialisName
           FROM 
               BILRG_GroupSpesialis aa     
           """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<GroupSpesialisType>(sql);
        return result;
    }
}
