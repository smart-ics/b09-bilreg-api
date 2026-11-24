using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;

public interface IAmbulanceKomponenDal :
    IListData<AmbulanceKomponenModel, IAmbulanceKey>
{
    void Insert(IEnumerable<AmbulanceKomponenModel> listModel);
    void Delete(IAmbulanceKey key);
}

public class AmbulanceKomponenDal : IAmbulanceKomponenDal
{
    private readonly DatabaseOptions _opt;

    public AmbulanceKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<AmbulanceKomponenModel> listModel)
    {
        var list = listModel?.ToList() ?? [];
        if (list.Count == 0) return;

        const string sql = @"
            INSERT INTO ta_transport2 (fs_kd_transport, fs_kd_detil_tarif, fn_tarif, fb_tetap)
            VALUES (@fs_kd_transport, @fs_kd_detil_tarif, @fn_tarif, @fb_tetap)";

        var paramList = list.Select(x => new
        {
            fs_kd_transport = x.AmbulanceId,
            fs_kd_detil_tarif = x.KomponenId,
            fn_tarif = x.NilaiTarif,
            fb_tetap = x.IsTetap
        });

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, paramList);
    }

    public void Delete(IAmbulanceKey key)
    {
        const string sql = @"
            DELETE FROM ta_transport2
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
        var list = conn.Read<AmbulanceKomponenDto>(sql, dp);
        return list?.Select(x => new AmbulanceKomponenModel(x.fs_kd_transport, x.fs_kd_detil_tarif, x.fn_tarif, x.fb_tetap)) ?? [];
    }
}

public class AmbulanceKomponenDto
{
    public string fs_kd_transport { get; set; } = string.Empty;
    public string fs_kd_detil_tarif { get; set; } = string.Empty;
    public decimal fn_tarif { get; set; }
    public bool fb_tetap { get; set; }
}
