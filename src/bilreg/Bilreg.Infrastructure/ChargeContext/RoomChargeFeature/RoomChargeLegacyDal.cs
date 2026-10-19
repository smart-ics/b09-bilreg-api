using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.CleanArchHelper;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public interface IRoomChargeLegacyDal : ISaveChange<RoomChargeModel>
{
}

public class RoomChargeLegacyDal : IRoomChargeLegacyDal
{
    private readonly DatabaseOptions _opt;

    public RoomChargeLegacyDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void SaveChanges(RoomChargeModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        var trsId = Truncate(model.RoomChargeId, 15);

        // 1. Check if record exists in legacy ta_trs_roomcharge
        const string checkSql = "SELECT COUNT(1) FROM ta_trs_roomcharge WHERE fs_kd_trs = @fs_kd_trs";
        var dpCheck = new DynamicParameters();
        dpCheck.AddParam("@fs_kd_trs", trsId, SqlDbType.VarChar);
        var exists = conn.ExecuteScalar<int>(checkSql, dpCheck) > 0;

        var ket = string.IsNullOrWhiteSpace(model.ReversalReason)
            ? model.RoomChargeStatus.ToString()
            : $"{model.RoomChargeStatus}: {model.ReversalReason}";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", trsId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pakai_bed", Truncate(model.PakaiBedId, 12), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", Truncate(model.ChargeCycleDate, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", model.TimeCharge.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", Truncate(model.AuditTrail.Created.UserId, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", Truncate(model.RegId, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", Truncate(model.BangsalId, 5), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bed", Truncate(model.BedId, 8), SqlDbType.VarChar);
        dp.AddParam("@fn_tarif", model.BaseTariff, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", model.DiscountAmount, SqlDbType.Decimal);
        dp.AddParam("@fn_total", model.TotalAmount, SqlDbType.Decimal);
        dp.AddParam("@fs_ket", Truncate(ket, 40), SqlDbType.VarChar);
        dp.AddParam("@fn_qty", model.ChargedUnits, SqlDbType.Decimal);
        dp.AddParam("@fb_gen_nm_kelas", true, SqlDbType.Bit);
        dp.AddParam("@fs_kd_tipe_jaminan", Truncate(model.PayerClassId, 5), SqlDbType.VarChar);
        dp.AddParam("@fn_nilai_klaim", 0m, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_trs_dx", string.Empty, SqlDbType.VarChar);

        if (!exists)
        {
            const string insertSql = """
                INSERT INTO ta_trs_roomcharge (
                    fs_kd_trs, fs_kd_pakai_bed, fd_tgl_trs, fs_jam_trs,
                    fs_kd_petugas, fs_kd_reg, fs_kd_layanan, fs_kd_bed,
                    fn_tarif, fn_diskon, fn_total, fs_ket, fn_qty,
                    fb_gen_nm_kelas, fs_kd_tipe_jaminan, fn_nilai_klaim, fs_kd_trs_dx)
                VALUES (
                    @fs_kd_trs, @fs_kd_pakai_bed, @fd_tgl_trs, @fs_jam_trs,
                    @fs_kd_petugas, @fs_kd_reg, @fs_kd_layanan, @fs_kd_bed,
                    @fn_tarif, @fn_diskon, @fn_total, @fs_ket, @fn_qty,
                    @fb_gen_nm_kelas, @fs_kd_tipe_jaminan, @fn_nilai_klaim, @fs_kd_trs_dx)
                """;
            conn.Execute(insertSql, dp);
        }
        else
        {
            const string updateSql = """
                UPDATE ta_trs_roomcharge
                SET
                    fs_kd_pakai_bed = @fs_kd_pakai_bed,
                    fd_tgl_trs = @fd_tgl_trs,
                    fs_jam_trs = @fs_jam_trs,
                    fs_kd_petugas = @fs_kd_petugas,
                    fs_kd_reg = @fs_kd_reg,
                    fs_kd_layanan = @fs_kd_layanan,
                    fs_kd_bed = @fs_kd_bed,
                    fn_tarif = @fn_tarif,
                    fn_diskon = @fn_diskon,
                    fn_total = @fn_total,
                    fs_ket = @fs_ket,
                    fn_qty = @fn_qty,
                    fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan
                WHERE fs_kd_trs = @fs_kd_trs
                """;
            conn.Execute(updateSql, dp);
        }

        // 2. Project components to ta_trs_roomcharge2
        const string deleteKompSql = "DELETE FROM ta_trs_roomcharge2 WHERE fs_kd_trs = @fs_kd_trs";
        var dpDel = new DynamicParameters();
        dpDel.AddParam("@fs_kd_trs", trsId, SqlDbType.VarChar);
        conn.Execute(deleteKompSql, dpDel);

        if (model.ListKomponen.Count > 0)
        {
            const string insertKompSql = """
                INSERT INTO ta_trs_roomcharge2 (
                    fs_kd_trs, fs_kd_detil_tarif,
                    fn_tarif, fn_diskon, fn_total,
                    fs_kd_rek_pdpt, fs_kd_rek_diskon)
                VALUES (
                    @fs_kd_trs, @fs_kd_detil_tarif,
                    @fn_tarif, @fn_diskon, @fn_total,
                    @fs_kd_rek_pdpt, @fs_kd_rek_diskon)
                """;
            var kompRows = model.ListKomponen.Select(k =>
            {
                var rowDp = new DynamicParameters();
                rowDp.AddParam("@fs_kd_trs", trsId, SqlDbType.VarChar);
                rowDp.AddParam("@fs_kd_detil_tarif", Truncate(k.KomponenId, 3), SqlDbType.VarChar);
                rowDp.AddParam("@fn_tarif", k.Tarif, SqlDbType.Decimal);
                rowDp.AddParam("@fn_diskon", k.Diskon, SqlDbType.Decimal);
                rowDp.AddParam("@fn_total", k.Total, SqlDbType.Decimal);
                rowDp.AddParam("@fs_kd_rek_pdpt", Truncate(k.RekeningPendapatan, 20), SqlDbType.VarChar);
                rowDp.AddParam("@fs_kd_rek_diskon", string.Empty, SqlDbType.VarChar);
                return rowDp;
            });
            conn.Execute(insertKompSql, kompRows);
        }

        // 3. Project to ta_trs_roomcharge_log if Reversed
        if (model.RoomChargeStatus == RoomChargeStatusEnum.Reversed)
        {
            const string insertLogSql = """
                INSERT INTO ta_trs_roomcharge_log (
                    fs_kd_trs, fd_tgl_trs, fd_tgl_upd, fs_jam_upd,
                    fs_kd_petugas, fs_kd_upd, fs_kd_reg,
                    fs_kd_layanan, fs_kd_bed,
                    fn_tarif, fn_qty, fb_status_void)
                VALUES (
                    @fs_kd_trs, @fd_tgl_trs, @fd_tgl_upd, @fs_jam_upd,
                    @fs_kd_petugas, @fs_kd_upd, @fs_kd_reg,
                    @fs_kd_layanan, @fs_kd_bed,
                    @fn_tarif, @fn_qty, @fb_status_void)
                """;
            var dpLog = new DynamicParameters();
            dpLog.AddParam("@fs_kd_trs", trsId, SqlDbType.VarChar);
            dpLog.AddParam("@fd_tgl_trs", Truncate(model.ChargeCycleDate, 10), SqlDbType.VarChar);
            dpLog.AddParam("@fd_tgl_upd", model.AuditTrail.Modified.Timestamp.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
            dpLog.AddParam("@fs_jam_upd", model.AuditTrail.Modified.Timestamp.ToString("HH:mm:ss"), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_petugas", Truncate(model.AuditTrail.Created.UserId, 10), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_upd", Truncate(model.AuditTrail.Modified.UserId, 10), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_reg", Truncate(model.RegId, 10), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_layanan", Truncate(model.BangsalId, 5), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_bed", Truncate(model.BedId, 8), SqlDbType.VarChar);
            dpLog.AddParam("@fn_tarif", model.BaseTariff, SqlDbType.Decimal);
            dpLog.AddParam("@fn_qty", model.ChargedUnits, SqlDbType.Decimal);
            dpLog.AddParam("@fb_status_void", true, SqlDbType.Bit);
            conn.Execute(insertLogSql, dpLog);
        }
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
