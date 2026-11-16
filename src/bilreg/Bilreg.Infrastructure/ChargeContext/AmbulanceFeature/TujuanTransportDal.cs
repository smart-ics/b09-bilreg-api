using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;

public interface ITujuanTransportDal :
    IInsert<TujuanTransportModel>,
    IUpdate<TujuanTransportModel>,
    IDelete<ITujuanTransportKey>,
    IGetData<TujuanTransportModel, ITujuanTransportKey>,
    IListData<TujuanTransportModel>
{
}

public class TujuanTransportDal : ITujuanTransportDal
{
    private readonly DatabaseOptions _opt;

    public TujuanTransportDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TujuanTransportModel model)
    {
        const string sql = @"
            INSERT INTO ta_tujuan_transport (
                fs_kd_tujuan_transport, fs_nm_tujuan_transport, fn_konstanta, fb_perkiraan, fs_kd_default_transport)
            VALUES (
                @fs_kd_tujuan_transport, @fs_nm_tujuan_transport, @fn_konstanta, @fb_perkiraan, @fs_kd_default_transport)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tujuan_transport", model.TujuanTransportId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tujuan_transport", model.TujuanTransportName, SqlDbType.VarChar);
        dp.AddParam("@fn_konstanta", model.Konstanta, SqlDbType.Decimal);
        dp.AddParam("@fb_perkiraan", model.IsPerkiraan, SqlDbType.Bit);
        dp.AddParam("@fs_kd_default_transport", model.DefaultAmbulanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TujuanTransportModel model)
    {
        const string sql = @"
            UPDATE ta_tujuan_transport
            SET fs_nm_tujuan_transport = @fs_nm_tujuan_transport,
                fn_konstanta = @fn_konstanta,
                fb_perkiraan = @fb_perkiraan,
                fs_kd_default_transport = @fs_kd_default_transport
            WHERE fs_kd_tujuan_transport = @fs_kd_tujuan_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tujuan_transport", model.TujuanTransportId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tujuan_transport", model.TujuanTransportName, SqlDbType.VarChar);
        dp.AddParam("@fn_konstanta", model.Konstanta, SqlDbType.Decimal);
        dp.AddParam("@fb_perkiraan", model.IsPerkiraan, SqlDbType.Bit);
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
            SELECT fs_kd_tujuan_transport, fs_nm_tujuan_transport, fn_konstanta, fb_perkiraan, fs_kd_default_transport
            FROM ta_tujuan_transport
            WHERE fs_kd_tujuan_transport = @fs_kd_tujuan_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tujuan_transport", key.TujuanTransportId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dto = conn.ReadSingle<TujuanTransportDto>(sql, dp);
        return dto?.ToModel()!;
    }

    public IEnumerable<TujuanTransportModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_tujuan_transport, fs_nm_tujuan_transport, fn_konstanta, fb_perkiraan, fs_kd_default_transport
            FROM ta_tujuan_transport";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<TujuanTransportDto>(sql);
        return list?.Select(x => x.ToModel()) ?? [];
    }
}

public class TujuanTransportDto
{
    public string fs_kd_tujuan_transport { get; set; } = string.Empty;
    public string fs_nm_tujuan_transport { get; set; } = string.Empty;
    public decimal fn_konstanta { get; set; }
    public bool fb_perkiraan { get; set; }
    public string fs_kd_default_transport { get; set; } = string.Empty;

    public TujuanTransportModel ToModel()
    {
        var model = new TujuanTransportModel(fs_kd_tujuan_transport, fs_nm_tujuan_transport, fn_konstanta, fb_perkiraan);
        model.SetDefaultAmbulance(fs_kd_default_transport);
        return model;
    }
}
