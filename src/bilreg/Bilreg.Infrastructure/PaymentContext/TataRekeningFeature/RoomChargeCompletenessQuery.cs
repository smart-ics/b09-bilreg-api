using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

/// <summary>
/// Implementasi pembacaan data pendukung validasi kelengkapan sewa kamar (Room Charge).
/// <para>
/// Interval okupansi tempat tidur dibaca dari RNA-BED (PakaiBed): data modern
/// <c>BILRG_RnaPakaiBedAlokasi</c> + <c>BILRG_RnaPakaiBedTransisi</c>, dengan fallback ke
/// tabel legacy <c>ta_trs_bed</c>. Catatan Room Charge yang sudah dibebankan dibaca dari
/// billing room charge pada <c>ta_trs_billing2</c> yang ditautkan ke tabel Room Charge
/// modern <c>BILRG_RnaRoomCharge</c> maupun legacy <c>ta_trs_roomcharge</c>.
/// </para>
/// </summary>
public class RoomChargeCompletenessQuery : IRoomChargeCompletenessQuery
{
    private const string EmptyDate = "3000-01-01";

    private readonly DatabaseOptions _opt;

    public RoomChargeCompletenessQuery(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public InpatientStayInterval? GetStayInterval(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId))
            return null;

        var modern = GetModernStayInterval(regId);
        if (modern is not null)
            return modern;

        return GetLegacyStayInterval(regId);
    }

    public IEnumerable<DateTime> ListBilledRoomChargeDates(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId))
            return [];

        var billedDates = new HashSet<DateTime>();

        foreach (var date in ListModernBilledRoomChargeDates(regId))
            billedDates.Add(date);

        foreach (var date in ListLegacyBilledRoomChargeDates(regId))
            billedDates.Add(date);

        return billedDates.OrderBy(x => x).ToList();
    }

    private InpatientStayInterval? GetModernStayInterval(string regId)
    {
        const string sql = """
            SELECT
                MIN(aa.StartedAt) AS AdmissionDate,
                MAX(CASE WHEN tt.PakaiBedTransition = 6 THEN tt.OccurredAt END) AS DischargeDate,
                MIN(CASE WHEN aa.PakaiBedStatus = 2 THEN 1 ELSE 0 END) AS HasOpenOccupancy
            FROM BILRG_RnaPakaiBedAlokasi aa
            LEFT JOIN BILRG_RnaPakaiBedTransisi tt
                   ON tt.PakaiBedId = aa.PakaiBedId
                  AND tt.PakaiBedTransition IN (5, 6)
            WHERE aa.RegId = @RegId
              AND aa.PakaiBedStatus IN (2, 3)
            """;

        try
        {
            var row = ReadStayInterval(sql, regId);
            if (row is null)
                return null;

            //okupansi yang masih aktif berarti bed belum benar-benar dilepas
            var isReleased = row.HasOpenOccupancy.GetValueOrDefault() == 0
                && row.DischargeDate.HasValue;

            return new InpatientStayInterval(
                regId,
                row.AdmissionDate!.Value,
                isReleased ? row.DischargeDate : null);
        }
        catch
        {
            // Tabel modern tidak tersedia, lanjutkan ke tabel legacy
            return null;
        }
    }

    private InpatientStayInterval? GetLegacyStayInterval(string regId)
    {
        const string sql = """
            SELECT
                MIN(TRY_CONVERT(DATETIME, aa.fd_tgl_in + ' ' + aa.fs_jam_in)) AS AdmissionDate,
                MAX(CASE WHEN aa.fd_tgl_out NOT IN ('', '3000-01-01')
                    THEN TRY_CONVERT(DATETIME, aa.fd_tgl_out + ' ' + aa.fs_jam_out) END) AS DischargeDate,
                MIN(CASE WHEN aa.fd_tgl_out IN ('', '3000-01-01') THEN 1 ELSE 0 END) AS HasOpenOccupancy
            FROM ta_trs_bed aa
            WHERE aa.fs_kd_reg = @RegId
              AND aa.fd_tgl_in NOT IN ('', '3000-01-01')
              AND (aa.fd_tgl_void IN ('', '3000-01-01') OR aa.fd_tgl_void IS NULL)
            """;

        try
        {
            var row = ReadStayInterval(sql, regId);
            if (row is null)
                return null;

            var isReleased = row.HasOpenOccupancy.GetValueOrDefault() == 0
                && row.DischargeDate.HasValue;

            return new InpatientStayInterval(
                regId,
                row.AdmissionDate!.Value,
                isReleased ? row.DischargeDate : null);
        }
        catch
        {
            return null;
        }
    }

    private StayIntervalRow? ReadStayInterval(string sql, string regId)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var row = conn.ReadSingle<StayIntervalRow>(sql, dp);
        if (row is null || row.AdmissionDate is null)
            return null;

        return row;
    }

    private IEnumerable<DateTime> ListModernBilledRoomChargeDates(string regId)
    {
        const string sql = """
            SELECT DISTINCT rc.ChargeCycleDate
            FROM ta_trs_billing2 aa
            INNER JOIN ta_trs_billing bb ON bb.fs_kd_trs = aa.fs_kd_trs
            INNER JOIN BILRG_RnaRoomCharge rc ON rc.RoomChargeId = bb.fs_kd_ref_biaya
            WHERE bb.fs_kd_reg = @RegId
              AND rc.RoomChargeStatus <> 'Reversed'
              AND rc.ChargeCycleDate NOT IN ('', '3000-01-01')
            """;

        return ReadBilledDates(sql, regId);
    }

    private IEnumerable<DateTime> ListLegacyBilledRoomChargeDates(string regId)
    {
        // fs_kd_trs pada ta_trs_roomcharge adalah RoomChargeId yang dipotong 15 karakter
        const string sql = """
            SELECT DISTINCT lrc.fd_tgl_trs
            FROM ta_trs_billing2 aa
            INNER JOIN ta_trs_billing bb ON bb.fs_kd_trs = aa.fs_kd_trs
            INNER JOIN ta_trs_roomcharge lrc ON lrc.fs_kd_trs = LEFT(bb.fs_kd_ref_biaya, 15)
            WHERE bb.fs_kd_reg = @RegId
              AND lrc.fd_tgl_trs NOT IN ('', '3000-01-01')
              AND lrc.fs_ket NOT LIKE 'Reversed%'
            """;

        return ReadBilledDates(sql, regId);
    }

    private IEnumerable<DateTime> ReadBilledDates(string sql, string regId)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        try
        {
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var values = conn.Query<string>(sql, dp);

            var dates = new List<DateTime>();
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value) || value == EmptyDate)
                    continue;

                if (DateTime.TryParseExact(
                        value,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var date))
                    dates.Add(date.Date);
            }

            return dates;
        }
        catch
        {
            // Tabel modern tidak tersedia, abaikan sumber ini
            return [];
        }
    }

    private sealed class StayIntervalRow
    {
        public DateTime? AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public int? HasOpenOccupancy { get; set; }
    }
}
