using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegSub.KarcisAgg;

public class KarcisLayananDal: IKarcisLayananDal
{
    private readonly DatabaseOptions _opt;

    public KarcisLayananDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<KarcisLayananModel> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("KarcisId", "fs_kd_karcis");
        bcp.AddMap("LayananId", "fs_kd_layanan");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_karcis3";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IKarcisKey key)
    {
        const string sql = @"
            DELETE FROM
                ta_karcis3
            WHERE
                fs_kd_karcis = @fs_kd_karcis";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", key.KarcisId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<KarcisLayananModel> ListData(IKarcisKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_karcis, aa.fs_kd_layanan,
                ISNULL(bb.fs_nm_layanan, '') AS fs_nm_layanan
            FROM ta_karcis3 aa
                LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
            WHERE
                aa.fs_kd_karcis = @fs_kd_karcis";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", filter.KarcisId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KarcisLayananDto>(sql, dp);
    }
}

public class KarcisLayananDto() : KarcisLayananModel(string.Empty, string.Empty, string.Empty)
{
    public string fs_kd_karcis { get => KarcisId; set => KarcisId = value; }
    public string fs_kd_layanan { get => LayananId; set => LayananId = value; }
    public string fs_nm_layanan { get => LayananName; set => LayananName = value; }
}