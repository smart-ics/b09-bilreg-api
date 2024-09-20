using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegSub.KarcisAgg;

public class KarcisKomponenDal: IKarcisKomponenDal
{
    private readonly DatabaseOptions _opt;

    public KarcisKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<KarcisKomponenModel> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("KarcisId", "fs_kd_karcis");
        bcp.AddMap("KomponenId", "fs_kd_detil_tarif");
        bcp.AddMap("Nilai", "fn_tarif");

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

    public IEnumerable<KarcisKomponenModel> ListData(IKarcisKey filter)
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

public class KarcisKomponenDto() : KarcisKomponenModel(string.Empty, string.Empty, string.Empty, decimal.Zero)
{
    public string fs_kd_karcis {get => KarcisId; set => KarcisId = value;}
    public string fs_kd_detil_tarif {get => KomponenId; set => KomponenId = value;}
    public decimal fn_tarif {get => Nilai; set => Nilai = value;}
    public string fs_nm_detil_tarif {get => KomponenName; set => KomponenName = value;}
}