using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public sealed class AdmisiRajalOfficerWorklistReferenceReader
    : IAdmisiRajalOfficerWorklistReferenceReader
{
    private readonly DatabaseOptions _options;

    public AdmisiRajalOfficerWorklistReferenceReader(IOptions<DatabaseOptions> options) =>
        _options = options.Value;

    public IReadOnlyList<AdmisiRajalOfficerWorklistReferenceView> List(
        IReadOnlyCollection<AdmisiRajalOfficerWorklistEntryKey> entries)
    {
        if (entries.Count == 0) return [];

        var antrianIds = entries
            .Select(x => x.AntrianId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var noUrut = entries
            .Select(x => x.NoUrut)
            .Distinct()
            .ToArray();
        var requested = entries.ToHashSet();

        using var connection = new SqlConnection(ConnStringHelper.Get(_options));
        return (connection.Query<AdmisiRajalOfficerWorklistReferenceSource>(
                Sql,
                new { antrianIds, noUrut }) ?? [])
            .Where(x => requested.Contains(
                new AdmisiRajalOfficerWorklistEntryKey(x.AntrianId, x.NoUrut)))
            .Select(Resolve)
            .ToList();
    }

    internal static AdmisiRajalOfficerWorklistReferenceView Resolve(
        AdmisiRajalOfficerWorklistReferenceSource source)
    {
        var bookingId =
            Real(source.AssistanceBookingId)
            ?? Real(source.TrackerBookingId)
            ?? Real(source.QueueBookingId);
        var registrationId =
            Real(source.OutcomeRegistrationId)
            ?? Real(source.QueueRegistrationId)
            ?? Real(source.TrackerRegistrationId)
            ?? Real(source.BookingRegistrationId);

        return new AdmisiRajalOfficerWorklistReferenceView(
            source.AntrianId,
            source.NoUrut,
            new AdmisiRajalOfficerWorklistReferences(bookingId, registrationId));
    }

    private static string? Real(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) || normalized == "-" ? null : normalized;
    }

    private const string Sql = """
        SELECT
            entry.AntrianId,
            entry.NoUrut,
            assistance.BookingId AS AssistanceBookingId,
            bookingEvent.ReffId AS TrackerBookingId,
            CASE
                WHEN UPPER(LTRIM(RTRIM(entry.ReffDesc))) IN ('BOK', 'BOK-AST')
                    THEN entry.ReffId
                ELSE NULL
            END AS QueueBookingId,
            outcome.RegId AS OutcomeRegistrationId,
            CASE
                WHEN UPPER(LTRIM(RTRIM(entry.ReffDesc))) = 'REG'
                    THEN entry.ReffId
                ELSE NULL
            END AS QueueRegistrationId,
            registrationEvent.ReffId AS TrackerRegistrationId,
            booking.RegId AS BookingRegistrationId
        FROM BILRG_AntrianEntry entry
        OUTER APPLY (
            SELECT TOP 1 assistance.BookingId
            FROM BILRG_AdmBookingAssistance assistance
            WHERE assistance.AntrianId = entry.AntrianId
              AND assistance.NoUrut = entry.NoUrut
              AND assistance.IsActive = 1
            ORDER BY assistance.UpdDate DESC, assistance.BookingId
        ) assistance
        OUTER APPLY (
            SELECT TOP 1 trackerEvent.ReffId
            FROM BILRG_PasienTrackerEvent trackerEvent
            WHERE trackerEvent.PasienTrackerId = entry.PasienTrackerId
              AND UPPER(LTRIM(RTRIM(trackerEvent.EventName))) = 'BOOKING'
            ORDER BY trackerEvent.NoUrut DESC
        ) bookingEvent
        OUTER APPLY (
            SELECT TOP 1 trackerEvent.ReffId
            FROM BILRG_PasienTrackerEvent trackerEvent
            WHERE trackerEvent.PasienTrackerId = entry.PasienTrackerId
              AND UPPER(LTRIM(RTRIM(trackerEvent.EventName))) = 'REGISTER'
            ORDER BY trackerEvent.NoUrut DESC
        ) registrationEvent
        OUTER APPLY (
            SELECT TOP 1 outcome.RegId
            FROM BILRG_RegOutcome outcome
            WHERE outcome.AntrianId = entry.AntrianId
              AND outcome.NoUrut = entry.NoUrut
              AND outcome.OutcomeType = 1
              AND outcome.VodDate = '3000-01-01'
            ORDER BY outcome.UpdDate DESC, outcome.OutcomeId
        ) outcome
        OUTER APPLY (
            SELECT COALESCE(
                NULLIF(NULLIF(LTRIM(RTRIM(assistance.BookingId)), ''), '-'),
                NULLIF(NULLIF(LTRIM(RTRIM(bookingEvent.ReffId)), ''), '-'),
                CASE
                    WHEN UPPER(LTRIM(RTRIM(entry.ReffDesc))) IN ('BOK', 'BOK-AST')
                        THEN NULLIF(NULLIF(LTRIM(RTRIM(entry.ReffId)), ''), '-')
                    ELSE NULL
                END
            ) AS BookingId
        ) bookingReference
        LEFT JOIN BILRG_Booking booking
            ON booking.BookingId = bookingReference.BookingId
            AND booking.VodDate = '3000-01-01'
        WHERE entry.AntrianId IN @antrianIds
          AND entry.NoUrut IN @noUrut
        """;
}

internal sealed record AdmisiRajalOfficerWorklistReferenceSource(
    string AntrianId,
    int NoUrut,
    string? AssistanceBookingId,
    string? TrackerBookingId,
    string? QueueBookingId,
    string? OutcomeRegistrationId,
    string? QueueRegistrationId,
    string? TrackerRegistrationId,
    string? BookingRegistrationId);
