using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public sealed class AdmisiRajalOfficerWorklistProjection
    : IAdmisiRajalOfficerWorklistProjection
{
    private const int CommandTimeoutSeconds = 30;
    private readonly DatabaseOptions _opt;

    public AdmisiRajalOfficerWorklistProjection(IOptions<DatabaseOptions> opt) =>
        _opt = opt.Value;

    public AdmisiRajalOfficerWorklistPage ListPage(
        AdmisiRajalOfficerWorklistFilter filter)
    {
        using var connection = new SqlConnection(ConnStringHelper.Get(_opt));
        using var result = connection.QueryMultiple(
            Sql,
            new
            {
                BusinessDate = filter.BusinessDate.ToDateTime(TimeOnly.MinValue),
                filter.ServicePointId,
                filter.QueueStatus,
                filter.LoketKey,
                filter.ActiveOnly,
                filter.Search,
                filter.QueueNumber,
                filter.PatientName,
                filter.PasienId,
                filter.BookingId,
                filter.RegId,
                filter.Source,
                filter.ArrivalFrom,
                filter.ArrivalTo,
                filter.LayananId,
                filter.DokterId,
                JamPraktekFrom = filter.JamPraktekFrom?.ToString("HH:mm"),
                JamPraktekTo = filter.JamPraktekTo?.ToString("HH:mm"),
                filter.SortBy,
                filter.SortDirection,
                filter.Offset,
                filter.Limit
            },
            commandTimeout: CommandTimeoutSeconds);

        var totalCount = checked((int)result.ReadSingle<long>());
        var rows = result.Read<WorklistRow>().ToList();
        var items = rows.Select(ToItem).ToList();
        var nextOffset = filter.Offset + items.Count;
        var hasMore = nextOffset < totalCount;
        return new AdmisiRajalOfficerWorklistPage(
            items,
            hasMore,
            hasMore ? nextOffset : null,
            totalCount);
    }

    private static AdmisiRajalOfficerWorklistItem ToItem(WorklistRow row)
    {
        var queue = new AdmissionQueueWorklistItem(
            row.AntrianId,
            row.NoUrut,
            EmptyToNull(row.QueueLabel),
            row.ServicePointId,
            row.ServicePointName,
            row.QueueStatus,
            row.Priority,
            (AdmissionQueueCreationReason)row.CreationReason,
            row.CallCount,
            EmptyToNull(row.LoketKey),
            row.ClaimState is null
                ? null
                : (AdmissionQueueClaimState)row.ClaimState,
            row.CreatedAt,
            row.ServedAt,
            row.DoneAt,
            EmptyToNull(row.PasienTrackerId));

        var identity = RealOrNull(row.IdentityPersonName) is not null
                       && RealOrNull(row.IdentityTglLahir) is not null
            ? new AdmisiRajalOfficerWorklistIdentity(
                RealOrNull(row.IdentityPersonName)!,
                RealOrNull(row.IdentityTglLahir)!,
                RealOrNull(row.EffectivePasienId))
            : null;

        var booking = row.HasBookingDetail
            ? new AdmisiRajalOfficerWorklistBooking(
                row.EffectiveBookingId!,
                row.BookingTglBerobat!,
                row.BookingJamPraktek!,
                row.BookingLayananId!,
                row.BookingLayananName!,
                row.BookingDokterId!,
                row.BookingDokterName!,
                RealOrNull(row.BookingPasienId),
                RealOrNull(row.BookingAsuransiName),
                RealOrNull(row.BookingNoPeserta))
            : null;

        var registration = row.HasRegistrationDetail
            ? new AdmisiRajalOfficerWorklistRegistration(
                row.EffectiveRegId!,
                row.RegistrationDate!,
                row.RegistrationPasienId!,
                row.RegistrationPasienName!,
                row.RegistrationLayananId!,
                row.RegistrationLayananName!,
                row.RegistrationDokterId!,
                row.RegistrationDokterName!)
            : null;

        var card = new AdmisiRajalOfficerWorklistCard(
            new AdmisiRajalOfficerQueueNumber(
                RealOrNull(row.EffectiveQueueNumber) ?? $"#{row.NoUrut}",
                row.NoUrut),
            RealOrNull(row.EffectivePatientName),
            RealOrNull(row.EffectivePasienId),
            RealOrNull(row.EffectiveBookingId),
            RealOrNull(row.EffectiveRegId),
            new AdmisiRajalOfficerCardReference(
                row.ServicePointId,
                row.ServicePointName),
            row.EffectiveSource,
            row.CreatedAt,
            ReferenceOrNull(row.EffectiveLayananId, row.EffectiveLayananName),
            ReferenceOrNull(row.EffectiveDokterId, row.EffectiveDokterName),
            RealOrNull(row.EffectiveJamPraktek));

        return new AdmisiRajalOfficerWorklistItem(
            queue,
            identity,
            booking,
            registration,
            card);
    }

    private static AdmisiRajalOfficerCardReference? ReferenceOrNull(
        string? id,
        string? name)
    {
        var resolvedId = RealOrNull(id);
        var resolvedName = RealOrNull(name);
        return resolvedId is null || resolvedName is null
            ? null
            : new AdmisiRajalOfficerCardReference(resolvedId, resolvedName);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? RealOrNull(string? value)
    {
        var resolved = EmptyToNull(value);
        return resolved == "-" ? null : resolved;
    }

    private const string Sql = """
        SET NOCOUNT ON;

        SELECT
            q.AntrianId,
            e.NoUrut,
            CASE
                WHEN q.QueuePrefixSnapshot = '' THEN NULL
                ELSE q.QueuePrefixSnapshot
                     + RIGHT('0000' + CONVERT(VARCHAR(4), e.NoUrut), 4)
            END AS QueueLabel,
            q.ServicePointCode AS ServicePointId,
            q.AntrianDescription AS ServicePointName,
            e.AntrianStatus AS QueueStatus,
            e.Priority,
            e.CreationReason,
            e.CallCount,
            c.LoketKey,
            c.ClaimState,
            e.CreatedAt,
            NULLIF(e.ServedAt, '3000-01-01') AS ServedAt,
            NULLIF(e.DoneAt, '3000-01-01') AS DoneAt,
            NULLIF(NULLIF(LTRIM(RTRIM(e.PasienTrackerId)), ''), '-') AS PasienTrackerId,

            COALESCE(
                NULLIF(NULLIF(LTRIM(RTRIM(rp.fs_nm_pasien)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(b.PasienName)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(pt.PersonName)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(e.PersonName)), ''), '-')
            ) AS IdentityPersonName,
            COALESCE(
                NULLIF(CONVERT(VARCHAR(10), rp.fd_tgl_lahir, 23), '3000-01-01'),
                NULLIF(CONVERT(VARCHAR(10), b.TglLahir, 23), '3000-01-01'),
                NULLIF(CONVERT(VARCHAR(10), pt.TglLahir, 23), '3000-01-01')
            ) AS IdentityTglLahir,

            CAST(IIF(b.BookingId IS NULL, 0, 1) AS BIT) AS HasBookingDetail,
            ids.BookingId AS EffectiveBookingId,
            CONVERT(VARCHAR(10), b.TglBerobat, 23) AS BookingTglBerobat,
            NULLIF(NULLIF(LTRIM(RTRIM(b.JamPraktek)), ''), '-') AS BookingJamPraktek,
            NULLIF(NULLIF(LTRIM(RTRIM(b.LayananId)), ''), '-') AS BookingLayananId,
            NULLIF(NULLIF(LTRIM(RTRIM(bl.fs_nm_layanan)), ''), '-') AS BookingLayananName,
            NULLIF(NULLIF(LTRIM(RTRIM(b.DokterId)), ''), '-') AS BookingDokterId,
            NULLIF(NULLIF(LTRIM(RTRIM(bd.fs_nm_peg)), ''), '-') AS BookingDokterName,
            NULLIF(NULLIF(LTRIM(RTRIM(b.PasienId)), ''), '-') AS BookingPasienId,
            NULLIF(NULLIF(LTRIM(RTRIM(b.AsuransiName)), ''), '-') AS BookingAsuransiName,
            NULLIF(NULLIF(LTRIM(RTRIM(b.NoPeserta)), ''), '-') AS BookingNoPeserta,

            CAST(IIF(r.fs_kd_reg IS NULL, 0, 1) AS BIT) AS HasRegistrationDetail,
            ids.RegId AS EffectiveRegId,
            NULLIF(NULLIF(LTRIM(RTRIM(r.fd_tgl_masuk)), ''), '-') AS RegistrationDate,
            NULLIF(NULLIF(LTRIM(RTRIM(r.fs_mr)), ''), '-') AS RegistrationPasienId,
            NULLIF(NULLIF(LTRIM(RTRIM(rp.fs_nm_pasien)), ''), '-') AS RegistrationPasienName,
            NULLIF(NULLIF(LTRIM(RTRIM(r.fs_kd_layanan)), ''), '-') AS RegistrationLayananId,
            NULLIF(NULLIF(LTRIM(RTRIM(rl.fs_nm_layanan)), ''), '-') AS RegistrationLayananName,
            NULLIF(NULLIF(LTRIM(RTRIM(r.fs_kd_medis)), ''), '-') AS RegistrationDokterId,
            NULLIF(NULLIF(LTRIM(RTRIM(rd.fs_nm_peg)), ''), '-') AS RegistrationDokterName,

            COALESCE(
                NULLIF(NULLIF(LTRIM(RTRIM(rp.fs_nm_pasien)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(b.PasienName)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(pt.PersonName)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(e.PersonName)), ''), '-')
            ) AS EffectivePatientName,
            COALESCE(
                NULLIF(NULLIF(LTRIM(RTRIM(r.fs_mr)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(b.PasienId)), ''), '-')
            ) AS EffectivePasienId,
            COALESCE(
                NULLIF(NULLIF(LTRIM(RTRIM(r.fs_kd_layanan)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(b.LayananId)), ''), '-')
            ) AS EffectiveLayananId,
            CASE
                WHEN NULLIF(NULLIF(LTRIM(RTRIM(r.fs_kd_layanan)), ''), '-') IS NOT NULL
                     AND NULLIF(NULLIF(LTRIM(RTRIM(rl.fs_nm_layanan)), ''), '-') IS NOT NULL
                    THEN NULLIF(NULLIF(LTRIM(RTRIM(rl.fs_nm_layanan)), ''), '-')
                WHEN NULLIF(NULLIF(LTRIM(RTRIM(b.LayananId)), ''), '-') IS NOT NULL
                     AND NULLIF(NULLIF(LTRIM(RTRIM(bl.fs_nm_layanan)), ''), '-') IS NOT NULL
                    THEN NULLIF(NULLIF(LTRIM(RTRIM(bl.fs_nm_layanan)), ''), '-')
                ELSE NULL
            END AS EffectiveLayananName,
            COALESCE(
                NULLIF(NULLIF(LTRIM(RTRIM(r.fs_kd_medis)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(b.DokterId)), ''), '-')
            ) AS EffectiveDokterId,
            CASE
                WHEN NULLIF(NULLIF(LTRIM(RTRIM(r.fs_kd_medis)), ''), '-') IS NOT NULL
                     AND NULLIF(NULLIF(LTRIM(RTRIM(rd.fs_nm_peg)), ''), '-') IS NOT NULL
                    THEN NULLIF(NULLIF(LTRIM(RTRIM(rd.fs_nm_peg)), ''), '-')
                WHEN NULLIF(NULLIF(LTRIM(RTRIM(b.DokterId)), ''), '-') IS NOT NULL
                     AND NULLIF(NULLIF(LTRIM(RTRIM(bd.fs_nm_peg)), ''), '-') IS NOT NULL
                    THEN NULLIF(NULLIF(LTRIM(RTRIM(bd.fs_nm_peg)), ''), '-')
                ELSE NULL
            END AS EffectiveDokterName,
            NULLIF(NULLIF(LTRIM(RTRIM(b.JamPraktek)), ''), '-') AS EffectiveJamPraktek,
            COALESCE(
                CASE
                    WHEN q.QueuePrefixSnapshot = '' THEN NULL
                    ELSE q.QueuePrefixSnapshot
                         + RIGHT('0000' + CONVERT(VARCHAR(4), e.NoUrut), 4)
                END,
                '#' + CONVERT(VARCHAR(10), e.NoUrut)
            ) AS EffectiveQueueNumber,
            CASE
                WHEN ids.BookingId IS NOT NULL THEN 'Booking'
                WHEN e.CreationReason = 0 THEN 'WalkIn'
                ELSE 'Unknown'
            END AS EffectiveSource
        INTO #ResolvedWorklist
        FROM BILRG_Antrian q
        INNER JOIN BILRG_AntrianEntry e
            ON e.AntrianId = q.AntrianId
        LEFT JOIN BILRG_AdmLoketCurrentCall c
            ON c.AntrianId = e.AntrianId
            AND c.NoUrut = e.NoUrut
            AND c.IsActive = 1
        LEFT JOIN BILRG_PasienTracker pt
            ON pt.PasienTrackerId = e.PasienTrackerId
        OUTER APPLY (
            SELECT TOP 1 a.BookingId
            FROM BILRG_AdmBookingAssistance a
            WHERE a.AntrianId = e.AntrianId
              AND a.NoUrut = e.NoUrut
              AND a.VodDate = '3000-01-01'
            ORDER BY a.UpdDate DESC, a.BookingId
        ) assistance
        OUTER APPLY (
            SELECT TOP 1 ev.ReffId
            FROM BILRG_PasienTrackerEvent ev
            WHERE ev.PasienTrackerId = e.PasienTrackerId
              AND UPPER(ev.EventName) = 'BOOKING'
              AND NULLIF(NULLIF(LTRIM(RTRIM(ev.ReffId)), ''), '-') IS NOT NULL
            ORDER BY ev.NoUrut DESC
        ) bookingEvent
        OUTER APPLY (
            SELECT TOP 1 ev.ReffId
            FROM BILRG_PasienTrackerEvent ev
            WHERE ev.PasienTrackerId = e.PasienTrackerId
              AND UPPER(ev.EventName) = 'REGISTER'
              AND NULLIF(NULLIF(LTRIM(RTRIM(ev.ReffId)), ''), '-') IS NOT NULL
            ORDER BY ev.NoUrut DESC
        ) registrationEvent
        LEFT JOIN BILRG_RegOutcome outcome
            ON outcome.AntrianId = e.AntrianId
            AND outcome.NoUrut = e.NoUrut
            AND outcome.OutcomeType = 1
            AND outcome.VodDate = '3000-01-01'
        CROSS APPLY (
            SELECT
                COALESCE(
                    NULLIF(NULLIF(LTRIM(RTRIM(assistance.BookingId)), ''), '-'),
                    NULLIF(NULLIF(LTRIM(RTRIM(bookingEvent.ReffId)), ''), '-'),
                    CASE
                        WHEN e.ReffDesc IN ('BOK', 'BOK-AST')
                            THEN NULLIF(NULLIF(LTRIM(RTRIM(e.ReffId)), ''), '-')
                        ELSE NULL
                    END
                ) AS BookingId,
                COALESCE(
                    NULLIF(NULLIF(LTRIM(RTRIM(outcome.RegId)), ''), '-'),
                    CASE
                        WHEN e.ReffDesc = 'REG'
                            THEN NULLIF(NULLIF(LTRIM(RTRIM(e.ReffId)), ''), '-')
                        ELSE NULL
                    END,
                    NULLIF(NULLIF(LTRIM(RTRIM(registrationEvent.ReffId)), ''), '-')
                ) AS InitialRegId
        ) initialIds
        LEFT JOIN BILRG_Booking b
            ON b.BookingId = initialIds.BookingId
            AND b.VodDate = '3000-01-01'
        CROSS APPLY (
            SELECT
                initialIds.BookingId AS BookingId,
                COALESCE(
                    initialIds.InitialRegId,
                    NULLIF(NULLIF(LTRIM(RTRIM(b.RegId)), ''), '-')
                ) AS RegId
        ) ids
        LEFT JOIN ta_registrasi r
            ON r.fs_kd_reg = ids.RegId
            AND r.fd_tgl_void = '3000-01-01'
        LEFT JOIN tc_mr rp
            ON rp.fs_mr = r.fs_mr
        LEFT JOIN ta_layanan rl
            ON rl.fs_kd_layanan = r.fs_kd_layanan
        LEFT JOIN td_peg rd
            ON rd.fs_kd_peg = r.fs_kd_medis
        LEFT JOIN ta_layanan bl
            ON bl.fs_kd_layanan = b.LayananId
        LEFT JOIN td_peg bd
            ON bd.fs_kd_peg = b.DokterId
        WHERE q.AntrianDate = @BusinessDate
          AND (@ServicePointId IS NULL OR q.ServicePointCode = @ServicePointId)
          AND (@QueueStatus IS NULL OR e.AntrianStatus = @QueueStatus)
          AND (@ActiveOnly = 0 OR e.AntrianStatus IN (0, 1))
          AND (@LoketKey IS NULL OR c.LoketKey = @LoketKey);

        SELECT *
        INTO #FilteredWorklist
        FROM #ResolvedWorklist w
        WHERE (
                @Search IS NULL
                OR w.EffectiveQueueNumber LIKE '%' + @Search + '%'
                OR w.EffectivePatientName LIKE '%' + @Search + '%'
                OR w.EffectivePasienId LIKE '%' + @Search + '%'
                OR w.EffectiveBookingId LIKE '%' + @Search + '%'
                OR w.EffectiveRegId LIKE '%' + @Search + '%'
                OR w.EffectiveLayananName LIKE '%' + @Search + '%'
                OR w.EffectiveDokterName LIKE '%' + @Search + '%'
              )
          AND (
                @QueueNumber IS NULL
                OR w.EffectiveQueueNumber LIKE @QueueNumber + '%'
                OR CONVERT(VARCHAR(10), w.NoUrut) = @QueueNumber
              )
          AND (@PatientName IS NULL OR w.EffectivePatientName LIKE '%' + @PatientName + '%')
          AND (@PasienId IS NULL OR w.EffectivePasienId = @PasienId)
          AND (@BookingId IS NULL OR w.EffectiveBookingId = @BookingId)
          AND (@RegId IS NULL OR w.EffectiveRegId = @RegId)
          AND (@Source IS NULL OR w.EffectiveSource = @Source)
          AND (@ArrivalFrom IS NULL OR w.CreatedAt >= @ArrivalFrom)
          AND (@ArrivalTo IS NULL OR w.CreatedAt <= @ArrivalTo)
          AND (@LayananId IS NULL OR w.EffectiveLayananId = @LayananId)
          AND (@DokterId IS NULL OR w.EffectiveDokterId = @DokterId)
          AND (
                @JamPraktekFrom IS NULL
                OR TRY_CONVERT(TIME, w.EffectiveJamPraktek) >= TRY_CONVERT(TIME, @JamPraktekFrom)
              )
          AND (
                @JamPraktekTo IS NULL
                OR TRY_CONVERT(TIME, w.EffectiveJamPraktek) <= TRY_CONVERT(TIME, @JamPraktekTo)
              );

        SELECT COUNT_BIG(1)
        FROM #FilteredWorklist;

        SELECT *
        FROM #FilteredWorklist w
        ORDER BY
            CASE WHEN @SortBy = 'queueOrder' THEN w.Priority END DESC,
            CASE WHEN @SortBy = 'queueOrder' THEN w.CreatedAt END ASC,
            CASE WHEN @SortBy = 'queueOrder' THEN w.NoUrut END ASC,
            CASE WHEN @SortBy = 'queueOrder' THEN w.AntrianId END ASC,

            CASE WHEN @SortBy = 'patientName' AND w.EffectivePatientName IS NULL THEN 1 ELSE 0 END,
            CASE WHEN @SortBy = 'pasienId' AND w.EffectivePasienId IS NULL THEN 1 ELSE 0 END,
            CASE WHEN @SortBy = 'bookingId' AND w.EffectiveBookingId IS NULL THEN 1 ELSE 0 END,
            CASE WHEN @SortBy = 'regId' AND w.EffectiveRegId IS NULL THEN 1 ELSE 0 END,
            CASE WHEN @SortBy = 'layanan' AND w.EffectiveLayananName IS NULL THEN 1 ELSE 0 END,
            CASE WHEN @SortBy = 'dokter' AND w.EffectiveDokterName IS NULL THEN 1 ELSE 0 END,
            CASE WHEN @SortBy = 'jamPraktek' AND w.EffectiveJamPraktek IS NULL THEN 1 ELSE 0 END,

            CASE WHEN @SortBy = 'queueNumber' AND @SortDirection = 'asc' THEN w.EffectiveQueueNumber END ASC,
            CASE WHEN @SortBy = 'queueNumber' AND @SortDirection = 'desc' THEN w.EffectiveQueueNumber END DESC,
            CASE WHEN @SortBy = 'patientName' AND @SortDirection = 'asc' THEN w.EffectivePatientName END ASC,
            CASE WHEN @SortBy = 'patientName' AND @SortDirection = 'desc' THEN w.EffectivePatientName END DESC,
            CASE WHEN @SortBy = 'pasienId' AND @SortDirection = 'asc' THEN w.EffectivePasienId END ASC,
            CASE WHEN @SortBy = 'pasienId' AND @SortDirection = 'desc' THEN w.EffectivePasienId END DESC,
            CASE WHEN @SortBy = 'bookingId' AND @SortDirection = 'asc' THEN w.EffectiveBookingId END ASC,
            CASE WHEN @SortBy = 'bookingId' AND @SortDirection = 'desc' THEN w.EffectiveBookingId END DESC,
            CASE WHEN @SortBy = 'regId' AND @SortDirection = 'asc' THEN w.EffectiveRegId END ASC,
            CASE WHEN @SortBy = 'regId' AND @SortDirection = 'desc' THEN w.EffectiveRegId END DESC,
            CASE WHEN @SortBy = 'servicePoint' AND @SortDirection = 'asc' THEN w.ServicePointName END ASC,
            CASE WHEN @SortBy = 'servicePoint' AND @SortDirection = 'desc' THEN w.ServicePointName END DESC,
            CASE WHEN @SortBy = 'source' AND @SortDirection = 'asc' THEN w.EffectiveSource END ASC,
            CASE WHEN @SortBy = 'source' AND @SortDirection = 'desc' THEN w.EffectiveSource END DESC,
            CASE WHEN @SortBy = 'arrivalTime' AND @SortDirection = 'asc' THEN w.CreatedAt END ASC,
            CASE WHEN @SortBy = 'arrivalTime' AND @SortDirection = 'desc' THEN w.CreatedAt END DESC,
            CASE WHEN @SortBy = 'layanan' AND @SortDirection = 'asc' THEN w.EffectiveLayananName END ASC,
            CASE WHEN @SortBy = 'layanan' AND @SortDirection = 'desc' THEN w.EffectiveLayananName END DESC,
            CASE WHEN @SortBy = 'dokter' AND @SortDirection = 'asc' THEN w.EffectiveDokterName END ASC,
            CASE WHEN @SortBy = 'dokter' AND @SortDirection = 'desc' THEN w.EffectiveDokterName END DESC,
            CASE WHEN @SortBy = 'jamPraktek' AND @SortDirection = 'asc' THEN w.EffectiveJamPraktek END ASC,
            CASE WHEN @SortBy = 'jamPraktek' AND @SortDirection = 'desc' THEN w.EffectiveJamPraktek END DESC,

            w.Priority DESC,
            w.CreatedAt ASC,
            w.NoUrut ASC,
            w.AntrianId ASC
        OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
        """;

    private sealed record WorklistRow(
        string AntrianId,
        int NoUrut,
        string? QueueLabel,
        string ServicePointId,
        string ServicePointName,
        int QueueStatus,
        bool Priority,
        int CreationReason,
        int CallCount,
        string? LoketKey,
        int? ClaimState,
        DateTime CreatedAt,
        DateTime? ServedAt,
        DateTime? DoneAt,
        string? PasienTrackerId,
        string? IdentityPersonName,
        string? IdentityTglLahir,
        bool HasBookingDetail,
        string? EffectiveBookingId,
        string? BookingTglBerobat,
        string? BookingJamPraktek,
        string? BookingLayananId,
        string? BookingLayananName,
        string? BookingDokterId,
        string? BookingDokterName,
        string? BookingPasienId,
        string? BookingAsuransiName,
        string? BookingNoPeserta,
        bool HasRegistrationDetail,
        string? EffectiveRegId,
        string? RegistrationDate,
        string? RegistrationPasienId,
        string? RegistrationPasienName,
        string? RegistrationLayananId,
        string? RegistrationLayananName,
        string? RegistrationDokterId,
        string? RegistrationDokterName,
        string? EffectivePatientName,
        string? EffectivePasienId,
        string? EffectiveLayananId,
        string? EffectiveLayananName,
        string? EffectiveDokterId,
        string? EffectiveDokterName,
        string? EffectiveJamPraktek,
        string EffectiveQueueNumber,
        string EffectiveSource);
}
