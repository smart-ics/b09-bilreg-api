using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public interface IProsedurMasukInapDal :
    IGetData<ProsedurMasukInapDto, IProsedurMasukInapKey>,
    IListData<ProsedurMasukInapDto>
{ }

public class ProsedurMasukInapDal : IProsedurMasukInapDal
{
    private readonly DatabaseOptions _opt;

    public ProsedurMasukInapDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public ProsedurMasukInapDto GetData(IProsedurMasukInapKey key)
    {
        const string sql = """
           SELECT 
           	    a.fs_kd_caramasuk_inap AS fs_kd_caramasuk_inap,
           	    a.fs_nm_caramasuk_inap AS fs_nm_caramasuk_inap,
           	    b.fs_kd_caramasuk_inap_dk AS fs_kd_caramasuk_inap_dk,
           	    b.fs_nm_caramasuk_inap_dk AS fs_nm_caramasuk_inap_dk
           FROM 
           	    ta_caramasuk_inap a
           	    LEFT JOIN ta_caramasuk_inap_dk b ON a.fs_kd_caramasuk_inap_dk = b.fs_kd_caramasuk_inap_dk      
           WHERE 
               a.fs_kd_caramasuk_inap = @fs_kd_caramasuk_inap
           """;


        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_caramasuk_inap", key.ProsedurMasukInapId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<ProsedurMasukInapDto>(sql, dp);
    }

    public IEnumerable<ProsedurMasukInapDto> ListData()
    {
        const string sql = """
            SELECT 
            	a.fs_kd_caramasuk_inap AS fs_kd_caramasuk_inap,
            	a.fs_nm_caramasuk_inap AS fs_nm_caramasuk_inap,
            	b.fs_kd_caramasuk_inap_dk AS fs_kd_caramasuk_inap_dk,
            	b.fs_nm_caramasuk_inap_dk AS fs_nm_caramasuk_inap_dk
            FROM 
            	ta_caramasuk_inap a
            	LEFT JOIN ta_caramasuk_inap_dk b ON a.fs_kd_caramasuk_inap_dk = b.fs_kd_caramasuk_inap_dk
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ProsedurMasukInapDto>(sql);
    }
}


