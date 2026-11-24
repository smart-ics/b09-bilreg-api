using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;

public interface IAmbulanceDal :
    IInsert<AmbulanceModel>,
    IUpdate<AmbulanceModel>,
    IDelete<IAmbulanceKey>,
    IGetData<AmbulanceModel, IAmbulanceKey>,
    IListData<AmbulanceModel>
{
}

public class AmbulanceDal : IAmbulanceDal
{
    private readonly DatabaseOptions _opt;

    public AmbulanceDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AmbulanceModel model)
    {
        const string sql = @"
            INSERT INTO ta_transport (
                fs_kd_transport, fs_nm_transport, fb_aktif, fn_abonemen,
                fs_no_polisi, fs_tipe_armada, fs_status_armada)
            VALUES (
                @fs_kd_transport, @fs_nm_transport, @fb_aktif, @fn_abonemen,
                @fs_no_polisi, @fs_tipe_armada, @fs_status_armada)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", model.AmbulanceId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_transport", model.AmbulanceName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fn_abonemen", model.Abonement, SqlDbType.Decimal);
        dp.AddParam("@fs_no_polisi", model.NoPolisi, SqlDbType.VarChar);
        dp.AddParam("@fs_tipe_armada", model.TipeArmada, SqlDbType.VarChar);
        dp.AddParam("@fs_status_armada", model.StatusArmada.ToString(), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AmbulanceModel model)
    {
        const string sql = @"
            UPDATE ta_transport 
            SET fs_nm_transport = @fs_nm_transport,
                fb_aktif = @fb_aktif,
                fn_abonemen = @fn_abonemen,
                fs_no_polisi = @fs_no_polisi,
                fs_tipe_armada = @fs_tipe_armada,
                fs_status_armada = @fs_status_armada
            WHERE fs_kd_transport = @fs_kd_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", model.AmbulanceId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_transport", model.AmbulanceName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fn_abonemen", model.Abonement, SqlDbType.Decimal);
        dp.AddParam("@fs_no_polisi", model.NoPolisi, SqlDbType.VarChar);
        dp.AddParam("@fs_tipe_armada", model.TipeArmada, SqlDbType.VarChar);
        dp.AddParam("@fs_status_armada", model.StatusArmada.ToString(), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAmbulanceKey key)
    {
        const string sql = @"
            DELETE FROM ta_transport
            WHERE fs_kd_transport = @fs_kd_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", key.AmbulanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public AmbulanceModel GetData(IAmbulanceKey key)
    {
        const string sql = @"
            SELECT fs_kd_transport, fs_nm_transport, fb_aktif, fn_abonemen,
                   fs_no_polisi, fs_tipe_armada, fs_status_armada
            FROM ta_transport
            WHERE fs_kd_transport = @fs_kd_transport";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", key.AmbulanceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dto = conn.ReadSingle<AmbulanceDto>(sql, dp);
        return dto?.ToModel()!;
    }

    public IEnumerable<AmbulanceModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_transport, fs_nm_transport, fb_aktif, fn_abonemen,
                   fs_no_polisi, fs_tipe_armada, fs_status_armada
            FROM ta_transport";

        var dp = new DynamicParameters();

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<AmbulanceDto>(sql, dp);
        return list?.Select(x => x.ToModel()) ?? [];
    }
}

public class AmbulanceDto
{
    public string fs_kd_transport { get; set; } = string.Empty;
    public string fs_nm_transport { get; set; } = string.Empty;
    public bool fb_aktif { get; set; }
    public decimal fn_abonemen { get; set; }
    public string fs_no_polisi { get; set; } = string.Empty;
    public string fs_tipe_armada { get; set; } = "Transport";
    public string fs_status_armada { get; set; } = "Available";

    public AmbulanceModel ToModel()
    {
        var model = new AmbulanceModel(
            fs_kd_transport,
            fs_nm_transport,
            fs_no_polisi,
            fs_tipe_armada,
            fs_status_armada);
        if (fb_aktif) model.SetAktif(); else model.UnSetAktif();
        model.SetAbonement(fn_abonemen);
        return model;
    }
}
