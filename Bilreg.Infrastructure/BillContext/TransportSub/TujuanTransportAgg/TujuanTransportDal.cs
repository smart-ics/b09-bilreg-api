using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;
using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TransportSub.TujuanTransportAgg;

public class TujuanTransportDal: ITujuanTransportDal
{
    private readonly DatabaseOptions _opt;

    public TujuanTransportDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TujuanTransportModel model)
    {
        const string sql = @"
            INSERT INTO ta_tujuan_transport (fs_kd_tujuan_transport, fs_nm_tujuan_transport, fn_konstanta, fb_perkiraan, fs_kd_default_transport)
            VALUES (@fs_kd_tujuan_transport, @fs_nm_tujuan_transport, @fn_konstanta, @fb_perkiraan, @fs_kd_default_transport)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tujuan_transport", model.TujuanTransportId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tujuan_transport", model.TujuanTransportName, SqlDbType.VarChar);
        dp.AddParam("@fn_konstanta", model.Konstanta, SqlDbType.VarChar);
        dp.AddParam("@fb_perkiraan", model.IsPerkiraan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_default_transport", model.DefaultAmbulanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TujuanTransportModel model)
    {
        const string sql = @"
            UPDATE ta_tujuan_transport
            SET 
                fs_nm_tujuan_transport = @fs_nm_tujuan_transport,
                fn_konstanta = @fn_konstanta,
                fb_perkiraan = @fb_perkiraan,
                fs_kd_default_transport = @fs_kd_default_transport
            WHERE 
                fs_kd_tujuan_transport = @fs_kd_tujuan_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tujuan_transport", model.TujuanTransportId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tujuan_transport", model.TujuanTransportName, SqlDbType.VarChar);
        dp.AddParam("@fn_konstanta", model.Konstanta, SqlDbType.VarChar);
        dp.AddParam("@fb_perkiraan", model.IsPerkiraan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_default_transport", model.DefaultAmbulanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITujuanTransportKey key)
    {
        const string sql = @"
            DELETE FROM ta_tujuan_transport
            WHERE fs_kd_tujuan_transport = @fs_kd_tujuan_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tujuan_transport", key.TujuanTransportId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TujuanTransportModel GetData(ITujuanTransportKey key)
    {
        const string sql = @"
            SELECT
                fs_kd_tujuan_transport, fs_nm_tujuan_transport, fn_konstanta, fb_perkiraan, fs_kd_default_transport
            FROM
                ta_tujuan_transport
            WHERE 
                fs_kd_tujuan_transport = @fs_kd_tujuan_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tujuan_transport", key.TujuanTransportId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TujuanTransportDto>(sql, dp);
    }

    public IEnumerable<TujuanTransportModel> ListData()
    {
        const string sql = @"
            SELECT
                fs_kd_tujuan_transport, fs_nm_tujuan_transport, fn_konstanta, fb_perkiraan, fs_kd_default_transport
            FROM
                ta_tujuan_transport";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TujuanTransportDto>(sql);
    }
}

public class TujuanTransportDto() : TujuanTransportModel(string.Empty, string.Empty, decimal.Zero, false)
{
    public string fs_kd_tujuan_transport { get => TujuanTransportId; set => TujuanTransportId = value; }
    public string fs_nm_tujuan_transport { get => TujuanTransportName; set => TujuanTransportName = value; }
    public decimal fn_konstanta { get => Konstanta; set => Konstanta = value; }
    public bool fb_perkiraan { get => IsPerkiraan; set => IsPerkiraan = value; }
    public string fs_kd_default_transport { get => DefaultAmbulanceId; set => DefaultAmbulanceId = value; }
}