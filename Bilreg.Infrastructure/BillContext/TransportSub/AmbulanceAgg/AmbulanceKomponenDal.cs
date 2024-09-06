using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TransportSub.AmbulanceAgg;

public class AmbulanceKomponenDal: IAmbulanceKomponenDal
{
    private readonly DatabaseOptions _opt;

    public AmbulanceKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<AmbulanceKomponenModel> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("AmbulanceId", "fs_kd_transport");
        bcp.AddMap("KomponenId", "fs_kd_detil_tarif");
        bcp.AddMap("NilaiTarif", "fn_tarif");
        bcp.AddMap("IsTetap", "fb_tetap");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_transport2";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IAmbulanceKey key)
    {
        const string sql = @"
            DELETE 
            FROM ta_transport2
            WHERE fs_kd_transport = @fs_kd_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", key.AmbulanceId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<AmbulanceKomponenModel> ListData(IAmbulanceKey filter)
    {
        const string sql = @"
            SELECT fs_kd_transport, fs_kd_detil_tarif, fn_tarif, fb_tetap
            FROM ta_transport2
            WHERE fs_kd_transport = @fs_kd_transport";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", filter.AmbulanceId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AmbulanceKomponenDto>(sql, dp);
    }
}

public class AmbulanceKomponenDto() : AmbulanceKomponenModel(string.Empty, string.Empty, decimal.Zero, false)
{
    public string fs_kd_transport { get => AmbulanceId; set => AmbulanceId = value; }
    public string fs_kd_detil_tarif { get => KomponenId; set => KomponenId = value; }
    public decimal fn_tarif { get => NilaiTarif; set => NilaiTarif = value; }
    public bool fb_tetap { get => IsTetap; set => IsTetap = value; }
}