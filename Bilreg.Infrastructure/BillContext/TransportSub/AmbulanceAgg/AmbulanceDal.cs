using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TransportSub.AmbulanceAgg;

public class AmbulanceDal: IAmbulanceDal
{
    private readonly DatabaseOptions _opt;

    public AmbulanceDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AmbulanceModel model)
    {
        const string sql = @"
            INSERT INTO ta_transport (fs_kd_transport, fs_nm_transport, fb_aktif, fn_abonemen)
            VALUES (@fs_kd_transport, @fs_nm_transport, @fb_aktif, @fn_abonemen)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", model.AmbulanceId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_transport", model.AmbulanceName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fn_abonemen", model.Abonement, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AmbulanceModel model)
    {
        const string sql = @"
            UPDATE ta_transport 
            SET fs_nm_transport = @fs_nm_transport,
                fb_aktif = @fb_aktif,
                fn_abonemen = @fn_abonemen
            WHERE fs_kd_transport = @fs_kd_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", model.AmbulanceId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_transport", model.AmbulanceName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fn_abonemen", model.Abonement, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public AmbulanceModel GetData(IAmbulanceKey key)
    {
        const string sql = @"
            SELECT fs_kd_transport, fs_nm_transport, fb_aktif, fn_abonemen
            FROM ta_transport
            WHERE fs_kd_transport = @fs_kd_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", key.AmbulanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<AmbulanceDto>(sql, dp);
    }

    public IEnumerable<AmbulanceModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_transport, fs_nm_transport, fb_aktif, fn_abonemen
            FROM ta_transport";

        var dp = new DynamicParameters();

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AmbulanceDto>(sql, dp);
    }
}

public class AmbulanceDto() : AmbulanceModel(string.Empty, string.Empty)
{
    public string fs_kd_transport { get => AmbulanceId; set => AmbulanceId = value; }
    public string fs_nm_transport { get => AmbulanceName; set => AmbulanceName = value; }
    public bool fb_aktif { get => IsAktif; set => IsAktif = value; }
    public decimal fn_abonemen { get => Abonement; set => Abonement = value; }
}