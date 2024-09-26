using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.PolisAgg;

public class PolisCoverDal : IPolisCoverDal
{
    private readonly DatabaseOptions _opt;

    public PolisCoverDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PolisCoverModel> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("PolisId", "fs_kd_polis");
        bcp.AddMap("PasienId", "fs_mr");
        bcp.AddMap("Status", "fs_kd_status");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_polis_cover";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPolisKey key)
    {
        const string sql = @"
            DELETE FROM ta_polis_cover
            WHERE fs_kd_polis = @fs_kd_polis";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PolisCoverModel> ListData(IPolisKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_polis, aa.fs_mr, aa.fs_kd_status,
                ISNULL(bb.fs_nm_pasien, '') fs_nm_pasien
            FROM ta_polis_cover aa
                LEFT JOIN tc_mr bb ON aa.fs_mr = bb.fs_mr
            WHERE fs_kd_polis = @fs_kd_polis";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_polis", filter.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PolisCoverDto>(sql, dp);
    }
}

public class PolisCoverDto : PolisCoverModel
{
    public string fs_kd_polis { get => PolisId; set => PolisId = value; }
    public string fs_mr { get => PasienId; set => PasienId = value; }
    public string fs_nm_pasien { get => PasienName; set => PasienName = value; }
    public string fs_kd_status { get => Status; set => Status = value; }

    public PolisCoverDto() : base(string.Empty)
    {
    }
}