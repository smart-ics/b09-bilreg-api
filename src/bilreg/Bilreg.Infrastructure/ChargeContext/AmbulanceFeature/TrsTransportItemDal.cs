using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;

public interface ITrsTransportItemDal :
    IListData<TrsTransportItemModel, ITrsTransportKey>
{
    void Insert(IEnumerable<TrsTransportItemModel> listItems);
    void Delete(ITrsTransportKey key);
}

public class TrsTransportItemDal : ITrsTransportItemDal
{
    private readonly DatabaseOptions _opt;

    public TrsTransportItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<TrsTransportItemModel> listItems)
    {
        var list = listItems?.ToList() ?? [];
        if (list.Count == 0) return;

        const string sql = @"
            INSERT INTO ta_trs_transport2 (
                fs_kd_trs, fs_kd_detil_tarif, fn_tarif, fn_qty, fs_kd_petugas_medis,
                fn_diskon, fn_sub_total, fb_void, fs_keterangan)
            VALUES (
                @fs_kd_trs, @fs_kd_detil_tarif, @fn_tarif, @fn_qty, @fs_kd_petugas_medis,
                @fn_diskon, @fn_sub_total, @fb_void, @fs_keterangan)";

        var paramList = list.Select(x => new
        {
            fs_kd_trs = x.TrsTransportId,
            fs_kd_detil_tarif = x.DetilTarifId,
            fn_tarif = x.Tarif,
            fn_qty = x.Qty,
            fs_kd_petugas_medis = x.PetugasMedisId,
            fn_diskon = x.Diskon,
            fn_sub_total = x.SubTotal,
            fb_void = x.IsVoid,
            fs_keterangan = x.Keterangan
        });

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, paramList);
    }

    public void Delete(ITrsTransportKey key)
    {
        const string sql = @"
            DELETE FROM ta_trs_transport2
            WHERE fs_kd_trs = @fs_kd_trs";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.TrsTransportId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TrsTransportItemModel> ListData(ITrsTransportKey filter)
    {
        const string sql = @"
            SELECT fs_kd_trs, fs_kd_detil_tarif, fn_tarif, fn_qty, fs_kd_petugas_medis,
                   fn_diskon, fn_sub_total, fb_void, fs_keterangan
            FROM ta_trs_transport2
            WHERE fs_kd_trs = @fs_kd_trs";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", filter.TrsTransportId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<TrsTransportItemDto>(sql, dp);
        return list?.Select(x => x.ToModel()) ?? [];
    }
}

public class TrsTransportItemDto
{
    public string fs_kd_trs { get; set; } = string.Empty;
    public string fs_kd_detil_tarif { get; set; } = string.Empty;
    public decimal fn_tarif { get; set; }
    public decimal fn_qty { get; set; } = 1m;
    public string fs_kd_petugas_medis { get; set; } = string.Empty;
    public decimal fn_diskon { get; set; }
    public decimal fn_sub_total { get; set; }
    public bool fb_void { get; set; }
    public string fs_keterangan { get; set; } = string.Empty;

    public TrsTransportItemModel ToModel()
    {
        return new TrsTransportItemModel(
            fs_kd_trs,
            fs_kd_detil_tarif,
            fn_tarif,
            fn_qty,
            fs_kd_petugas_medis,
            fn_diskon,
            fs_keterangan,
            fb_void);
    }
}
