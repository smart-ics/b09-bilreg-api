using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public interface IKarcisKomponenDal :
    IInsertBulk<KarcisKomponenDto>,
    IDelete<IKarcisKey>,
    IListData<KarcisKomponenDto, IKarcisKey>
{
    
}

public class KarcisKomponenDal: IKarcisKomponenDal
{
    private readonly DatabaseOptions _opt;

    public KarcisKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<KarcisKomponenDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("fs_kd_karcis", "fs_kd_karcis");
        bcp.AddMap("fs_kd_detil_tarif", "fs_kd_detil_tarif");
        bcp.AddMap("fn_tarif", "fn_tarif");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_karcis2";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IKarcisKey key)
    {
        const string sql = @"
            DELETE FROM
                ta_karcis2
            WHERE
                fs_kd_karcis = @fs_kd_karcis";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", key.KarcisId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<KarcisKomponenDto> ListData(IKarcisKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_karcis, aa.fs_kd_detil_tarif, aa.fn_tarif,
                ISNULL(bb.fs_nm_detil_tarif, '') AS fs_nm_detil_tarif
            FROM ta_karcis2 aa
                LEFT JOIN ta_detil_tarif bb ON aa.fs_kd_detil_tarif = bb.fs_kd_detil_tarif
            WHERE
                aa.fs_kd_karcis = @fs_kd_karcis";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_karcis", filter.KarcisId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KarcisKomponenDto>(sql, dp);
    }
}
