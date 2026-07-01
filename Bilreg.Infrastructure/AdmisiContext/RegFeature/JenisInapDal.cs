using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public interface IJenisInapDal :
    IGetData<JenisInapDto, IJenisInapKey>,
    IListData<JenisInapDto>
{ }
public class JenisInapDal : IJenisInapDal
{
    private readonly DatabaseOptions _opt;

    public JenisInapDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public JenisInapDto GetData(IJenisInapKey key)
    {
        const string sql = """
           SELECT 
           	fs_kd_jenis_inap, fs_nm_jenis_inap
           FROM 
           	ta_jenis_inap a      
           WHERE 
               a.fs_kd_jenis_inap = @fs_kd_jenis_inap
           """;


        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_inap", key.JenisInapId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<JenisInapDto>(sql, dp);
    }

    public IEnumerable<JenisInapDto> ListData()
    {
        const string sql = """
            SELECT 
            	fs_kd_jenis_inap, fs_nm_jenis_inap
            FROM 
            	ta_jenis_inap a
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JenisInapDto>(sql);
    }
}


