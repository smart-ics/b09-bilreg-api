using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;

public interface ITransportTujuanDal :
    IListData<TransportTujuanModel, IAmbulanceKey>
{
    void Insert(IEnumerable<TransportTujuanModel> listModel);
    void Delete(IAmbulanceKey key);
    IEnumerable<TransportTujuanModel> ListData(ITujuanTransportKey filter);
}

public class TransportTujuanDal : ITransportTujuanDal
{
    private readonly DatabaseOptions _opt;

    public TransportTujuanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<TransportTujuanModel> listModel)
    {
        var list = listModel?.ToList() ?? [];
        if (list.Count == 0) return;

        const string sql = @"
            INSERT INTO ta_transport3 (fs_kd_transport, fs_kd_tujuan_transport, fn_konstanta)
            VALUES (@fs_kd_transport, @fs_kd_tujuan_transport, @fn_konstanta)";

        var paramList = list.Select(x => new
        {
            fs_kd_transport = x.AmbulanceId,
            fs_kd_tujuan_transport = x.TujuanTransportId,
            fn_konstanta = x.Konstanta
        });

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, paramList);
    }

    public void Delete(IAmbulanceKey key)
    {
        const string sql = @"
            DELETE FROM ta_transport3
            WHERE fs_kd_transport = @fs_kd_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", key.AmbulanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TransportTujuanModel> ListData(IAmbulanceKey filter)
    {
        const string sql = @"
            SELECT fs_kd_transport, fs_kd_tujuan_transport, fn_konstanta
            FROM ta_transport3
            WHERE fs_kd_transport = @fs_kd_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", filter.AmbulanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<TransportTujuanDto>(sql, dp);
        return list?.Select(x => x.ToModel()) ?? [];
    }

    public IEnumerable<TransportTujuanModel> ListData(ITujuanTransportKey filter)
    {
        const string sql = @"
            SELECT fs_kd_transport, fs_kd_tujuan_transport, fn_konstanta
            FROM ta_transport3
            WHERE fs_kd_tujuan_transport = @fs_kd_tujuan_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tujuan_transport", filter.TujuanTransportId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<TransportTujuanDto>(sql, dp);
        return list?.Select(x => x.ToModel()) ?? [];
    }
}

public class TransportTujuanDto
{
    public string fs_kd_transport { get; set; } = string.Empty;
    public string fs_kd_tujuan_transport { get; set; } = string.Empty;
    public decimal fn_konstanta { get; set; }

    public TransportTujuanModel ToModel() => new(fs_kd_transport, fs_kd_tujuan_transport, fn_konstanta);
}
