using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.CleanArchHelper;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public interface IRoomChargeLegacyDal : ISaveChange<RoomChargeModel>
{
    IEnumerable<RoomChargeLegacyDto> ListDataByReg(string regId);
}

public class RoomChargeLegacyDal : IRoomChargeLegacyDal
{
    private readonly DatabaseOptions _opt;
    private readonly ITglJamProvider? _tglJamProvider;

    public RoomChargeLegacyDal(IOptions<DatabaseOptions> opt, ITglJamProvider? tglJamProvider = null)
    {
        _opt = opt.Value;
        _tglJamProvider = tglJamProvider;
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
            try
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

                var fallbackNow = _tglJamProvider?.Now ?? DateTime.Now;
                var updTime = (model.AuditTrail.Modified.Timestamp == default || model.AuditTrail.Modified.Timestamp.Year < 1900)
                    ? fallbackNow
                    : model.AuditTrail.Modified.Timestamp;
                var updUser = string.IsNullOrWhiteSpace(model.AuditTrail.Modified.UserId)
                    ? (string.IsNullOrWhiteSpace(model.AuditTrail.Created.UserId) ? "SYSTEM" : model.AuditTrail.Created.UserId)
                    : model.AuditTrail.Modified.UserId;

                var dpLog = new DynamicParameters();
                dpLog.AddParam("@fs_kd_trs", trsId, SqlDbType.VarChar);
                dpLog.AddParam("@fd_tgl_trs", Truncate(model.ChargeCycleDate, 10), SqlDbType.VarChar);
                dpLog.AddParam("@fd_tgl_upd", updTime.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
                dpLog.AddParam("@fs_jam_upd", updTime.ToString("HH:mm:ss"), SqlDbType.VarChar);
                dpLog.AddParam("@fs_kd_petugas", Truncate(model.AuditTrail.Created.UserId, 10), SqlDbType.VarChar);
                dpLog.AddParam("@fs_kd_upd", Truncate(updUser, 10), SqlDbType.VarChar);
                dpLog.AddParam("@fs_kd_reg", Truncate(model.RegId, 10), SqlDbType.VarChar);
                dpLog.AddParam("@fs_kd_layanan", Truncate(model.BangsalId, 5), SqlDbType.VarChar);
                dpLog.AddParam("@fs_kd_bed", Truncate(model.BedId, 8), SqlDbType.VarChar);
                dpLog.AddParam("@fn_tarif", model.BaseTariff, SqlDbType.Decimal);
                dpLog.AddParam("@fn_qty", model.ChargedUnits, SqlDbType.Decimal);
                dpLog.AddParam("@fb_status_void", true, SqlDbType.Bit);
                conn.Execute(insertLogSql, dpLog);
            }
            catch
            {
                // Legacy log projection non-critical
            }
        }
    }

    public IEnumerable<RoomChargeLegacyDto> ListDataByReg(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId))
            return [];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        const string headerSql = """
            SELECT 
                aa.fs_kd_trs AS RoomChargeId,
                aa.fs_kd_pakai_bed AS PakaiBedId,
                aa.fs_kd_reg AS RegId,
                aa.fs_kd_layanan AS BangsalId,
                ISNULL(lay.fs_nm_layanan, '') AS BangsalName,
                ISNULL(bed.fs_kd_kamar, '') AS KamarId,
                ISNULL(kmr.fs_nm_kamar, '') AS KamarName,
                aa.fs_kd_bed AS BedId,
                ISNULL(bed.fs_nm_bed, aa.fs_kd_bed) AS BedCode,
                ISNULL(CONVERT(VARCHAR(10), aa.fd_tgl_trs, 120), '') AS ChargeCycleDate,
                ISNULL(aa.fs_jam_trs, '') AS JamTrs,
                ISNULL(aa.fs_kd_petugas, '') AS PetugasId,
                aa.fn_tarif AS BaseTariff,
                aa.fn_diskon AS DiscountAmount,
                aa.fn_total AS TotalAmount,
                aa.fn_qty AS ChargedUnits,
                ISNULL(kmr.fs_kd_kelas, '') AS PhysicalClassId,
                ISNULL(kls_kmr.fs_nm_kelas, '') AS PhysicalClassName,
                ISNULL(reg.fs_kd_kelas, ISNULL(aa.fs_kd_tipe_jaminan, '')) AS PayerClassId,
                ISNULL(kls_reg.fs_nm_kelas, '') AS PayerClassName,
                COALESCE(NULLIF(tb.fs_kd_kelas, ''), NULLIF(kmr.fs_kd_kelas, ''), NULLIF(reg.fs_kd_kelas, ''), NULLIF(aa.fs_kd_tipe_jaminan, ''), '') AS BillingClassId,
                COALESCE(NULLIF(kls_tb.fs_nm_kelas, ''), NULLIF(kls_kmr.fs_nm_kelas, ''), NULLIF(kls_reg.fs_nm_kelas, ''), '') AS BillingClassName,
                ISNULL(aa.fs_ket, '') AS Notes,
                CASE 
                    WHEN ISNULL(lg.fb_status_void, 0) = 1 THEN 'Reversed'
                    WHEN aa.fs_ket LIKE 'Reversed%' OR aa.fs_ket LIKE 'Void%' OR aa.fs_ket LIKE '%Void%' THEN 'Reversed'
                    ELSE 'Posted'
                END AS RoomChargeStatus
            FROM ta_trs_roomcharge aa
            LEFT JOIN ta_trs_bed tb ON aa.fs_kd_pakai_bed = tb.fs_kd_trs
            LEFT JOIN ta_registrasi reg ON aa.fs_kd_reg = reg.fs_kd_reg
            LEFT JOIN ta_bed bed ON aa.fs_kd_bed = bed.fs_kd_bed
            LEFT JOIN ta_kamar kmr ON bed.fs_kd_kamar = kmr.fs_kd_kamar
            LEFT JOIN ta_layanan lay ON aa.fs_kd_layanan = lay.fs_kd_layanan
            LEFT JOIN ta_kelas kls_tb ON tb.fs_kd_kelas = kls_tb.fs_kd_kelas
            LEFT JOIN ta_kelas kls_kmr ON kmr.fs_kd_kelas = kls_kmr.fs_kd_kelas
            LEFT JOIN ta_kelas kls_reg ON reg.fs_kd_kelas = kls_reg.fs_kd_kelas
            LEFT JOIN (
                SELECT fs_kd_trs, MAX(CASE WHEN fb_status_void = 1 THEN 1 ELSE 0 END) AS fb_status_void
                FROM ta_trs_roomcharge_log
                GROUP BY fs_kd_trs
            ) lg ON aa.fs_kd_trs = lg.fs_kd_trs
            WHERE aa.fs_kd_reg = @RegId
            ORDER BY aa.fd_tgl_trs ASC, aa.fs_jam_trs ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        var listHeader = conn.Query<RoomChargeLegacyDto>(headerSql, dp).ToList();
        if (listHeader.Count == 0)
            return [];

        const string kompSql = """
            SELECT 
                bb.fs_kd_trs AS RoomChargeId,
                bb.fs_kd_detil_tarif AS KomponenId,
                ISNULL(dt.fs_nm_detil_tarif, bb.fs_kd_detil_tarif) AS KomponenName,
                'Akomodasi' AS KomponenCategory,
                bb.fn_tarif AS Tarif,
                bb.fn_diskon AS Diskon,
                bb.fn_total AS Total,
                ISNULL(bb.fs_kd_rek_pdpt, '') AS RekeningPendapatan,
                ISNULL(bb.fs_kd_rek_diskon, '') AS RekeningDiskon
            FROM ta_trs_roomcharge2 bb
            INNER JOIN ta_trs_roomcharge aa ON bb.fs_kd_trs = aa.fs_kd_trs
            LEFT JOIN ta_detil_tarif dt ON bb.fs_kd_detil_tarif = dt.fs_kd_detil_tarif
            WHERE aa.fs_kd_reg = @RegId
            """;

        List<RoomChargeKomponenLegacyDto> listKomp;
        try
        {
            listKomp = conn.Query<RoomChargeKomponenLegacyDto>(kompSql, dp).ToList();
        }
        catch
        {
            listKomp = [];
        }

        var kompLookup = listKomp.GroupBy(x => x.RoomChargeId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var item in listHeader)
        {
            if (DateTime.TryParse($"{item.ChargeCycleDate} {item.JamTrs}", out var parsedDt))
            {
                item.TimeCharge = parsedDt;
            }
            else if (DateTime.TryParse(item.ChargeCycleDate, out var parsedD))
            {
                item.TimeCharge = parsedD;
            }

            if (kompLookup.TryGetValue(item.RoomChargeId, out var komps))
            {
                item.ListKomponen = komps;
            }
            else
            {
                item.ListKomponen = [];
            }
        }

        return listHeader;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
