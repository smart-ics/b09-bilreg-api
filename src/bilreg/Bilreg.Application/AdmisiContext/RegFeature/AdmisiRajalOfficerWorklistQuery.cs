using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public sealed record AdmisiRajalOfficerWorklistIdentity(
    string PersonName,
    string TglLahir,
    string? PasienId);

public sealed record AdmisiRajalOfficerWorklistBooking(
    string BookingId,
    string TglBerobat,
    string JamPraktek,
    string LayananId,
    string LayananName,
    string DokterId,
    string DokterName,
    string? PasienId,
    string? AsuransiName,
    string? NoPeserta);

public sealed record AdmisiRajalOfficerWorklistRegistration(
    string RegId,
    string RegDate,
    string PasienId,
    string PasienName,
    string LayananId,
    string LayananName,
    string DokterId,
    string DokterName);

public sealed record AdmisiRajalOfficerWorklistItem(
    AdmissionQueueWorklistItem Queue,
    AdmisiRajalOfficerWorklistIdentity? Identity,
    AdmisiRajalOfficerWorklistBooking? Booking,
    AdmisiRajalOfficerWorklistRegistration? Registration);

public sealed record AdmisiRajalOfficerWorklistPage(
    IReadOnlyList<AdmisiRajalOfficerWorklistItem> Items,
    bool HasMore,
    int? NextOffset,
    int TotalCount);

public record AdmisiRajalOfficerWorklistQuery(
    string BusinessDateYmd,
    string? ServicePointId = null,
    int? QueueStatus = null,
    string? LoketKey = null,
    int Offset = 0,
    int Limit = 100,
    bool ActiveOnly = false) : IRequest<AdmisiRajalOfficerWorklistPage>;

/// <summary>
/// Admisi-owned read composition over the Patient Tracker queue-only projection.
/// Does not persist a second worklist or mutate queue truth.
/// </summary>
public sealed class AdmisiRajalOfficerWorklistHandler
    : IRequestHandler<AdmisiRajalOfficerWorklistQuery, AdmisiRajalOfficerWorklistPage>
{
    public const string BookingEventName = "BOOKING";
    public const string RegisterEventName = "REGISTER";
    private static readonly Meter Meter = new("Bilreg.AdmisiRajal", "1.0");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "bilreg.admisi_rajal.worklist.duration",
        "ms");
    private static readonly Histogram<long> ResultCount = Meter.CreateHistogram<long>(
        "bilreg.admisi_rajal.worklist.result_count",
        "{entry}");

    private readonly IAdmissionQueueOperationalProjection _projection;
    private readonly IPasienTrackerRepo _trackers;
    private readonly IBookingRepo _bookings;
    private readonly IRegRepo _regs;
    private readonly IBookingAssistanceRepo _assistance;
    private readonly ILogger<AdmisiRajalOfficerWorklistHandler> _logger;

    public AdmisiRajalOfficerWorklistHandler(
        IAdmissionQueueOperationalProjection projection,
        IPasienTrackerRepo trackers,
        IBookingRepo bookings,
        IRegRepo regs,
        IBookingAssistanceRepo assistance,
        ILogger<AdmisiRajalOfficerWorklistHandler>? logger = null)
    {
        _projection = projection;
        _trackers = trackers;
        _bookings = bookings;
        _regs = regs;
        _assistance = assistance;
        _logger = logger ?? NullLogger<AdmisiRajalOfficerWorklistHandler>.Instance;
    }

    public Task<AdmisiRajalOfficerWorklistPage> Handle(
        AdmisiRajalOfficerWorklistQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessDateYmd)
            || !DateOnly.TryParseExact(
                request.BusinessDateYmd,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            throw new ArgumentException(
                "BusinessDateYmd must use yyyy-MM-dd.",
                nameof(request.BusinessDateYmd));
        if (request.Offset < 0) throw new ArgumentOutOfRangeException(nameof(request.Offset));
        if (request.Limit is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(request.Limit));
        if (request.Offset > int.MaxValue - request.Limit)
            throw new ArgumentOutOfRangeException(nameof(request.Offset));
        if (request.QueueStatus.HasValue
            && !Enum.IsDefined(typeof(AntrianStatusEnum), request.QueueStatus.Value))
            throw new ArgumentOutOfRangeException(nameof(request.QueueStatus));
        if (request.ActiveOnly && request.QueueStatus.HasValue)
            throw new ArgumentException(
                "ActiveOnly and QueueStatus cannot be combined.",
                nameof(request.ActiveOnly));

        var filter = new AdmissionQueueWorklistFilter(
            date, EmptyToNull(request.ServicePointId), request.QueueStatus,
            EmptyToNull(request.LoketKey), request.Offset, request.Limit, request.ActiveOnly);

        var totalWatch = Stopwatch.StartNew();
        var queueWatch = Stopwatch.StartNew();
        var queuePage = _projection.ListWorklistPage(filter);
        queueWatch.Stop();

        var enrichmentWatch = Stopwatch.StartNew();
        IReadOnlyList<AdmisiRajalOfficerWorklistItem> items = queuePage.Items
            .Select(Compose)
            .ToList();
        enrichmentWatch.Stop();
        totalWatch.Stop();
        var resultBucket = Bucket(items.Count);
        Duration.Record(
            totalWatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("active", request.ActiveOnly),
            new KeyValuePair<string, object?>("result_bucket", resultBucket));
        ResultCount.Record(
            items.Count,
            new KeyValuePair<string, object?>("result_bucket", resultBucket));

        _logger.LogInformation(
            "AdmisiRajal officer worklist activeOnly={ActiveOnly} offset={Offset} limit={Limit} returned={Returned} hasMore={HasMore} queueMs={QueueMs} enrichmentMs={EnrichmentMs} totalMs={TotalMs}",
            request.ActiveOnly,
            request.Offset,
            request.Limit,
            items.Count,
            queuePage.HasMore,
            queueWatch.ElapsedMilliseconds,
            enrichmentWatch.ElapsedMilliseconds,
            totalWatch.ElapsedMilliseconds);

        return Task.FromResult(new AdmisiRajalOfficerWorklistPage(
            items,
            queuePage.HasMore,
            queuePage.NextOffset,
            queuePage.TotalCount));
    }

    private AdmisiRajalOfficerWorklistItem Compose(AdmissionQueueWorklistItem queue)
    {
        string? bookingId = null;
        string? regId = null;
        AdmisiRajalOfficerWorklistIdentity? identity = null;

        if (PasienTrackerStableIdentity.IsRealTrackerId(queue.PasienTrackerId))
        {
            var trackerMaybe = _trackers.LoadEntity(PasienTrackerModel.Key(queue.PasienTrackerId!));
            if (trackerMaybe.HasValue)
            {
                var tracker = trackerMaybe.Value;
                identity = new AdmisiRajalOfficerWorklistIdentity(
                    tracker.Person.PersonName,
                    tracker.Person.TglLahir.ToString("yyyy-MM-dd"),
                    null);
                bookingId = LatestEventReff(tracker, BookingEventName);
                regId = LatestEventReff(tracker, RegisterEventName);
            }
        }

        bookingId ??= _assistance.FindActiveByEntry(queue.AntrianId, queue.NoUrut)?.BookingId;

        AdmisiRajalOfficerWorklistBooking? booking = null;
        if (!string.IsNullOrWhiteSpace(bookingId))
        {
            var bookingMaybe = _bookings.LoadEntity(BookingModel.Key(bookingId));
            if (bookingMaybe.HasValue)
            {
                var b = bookingMaybe.Value;
                booking = ToBooking(b);
                identity ??= new AdmisiRajalOfficerWorklistIdentity(
                    b.Person.PersonName,
                    b.Person.TglLahir.ToString("yyyy-MM-dd"),
                    RealOrNull(b.PasienId));
                if (string.IsNullOrWhiteSpace(regId) && RealOrNull(b.Reg.RegId) is not null)
                    regId = b.Reg.RegId;
            }
        }

        AdmisiRajalOfficerWorklistRegistration? registration = null;
        if (!string.IsNullOrWhiteSpace(regId))
        {
            var regMaybe = _regs.LoadEntity(RegModel.Key(regId));
            if (regMaybe.HasValue)
            {
                var r = regMaybe.Value;
                registration = ToRegistration(r);
                identity ??= new AdmisiRajalOfficerWorklistIdentity(
                    r.Pasien.PasienName,
                    r.Pasien.TglLahir.ToString("yyyy-MM-dd"),
                    RealOrNull(r.Pasien.PasienId));
                if (identity is not null && identity.PasienId is null)
                    identity = identity with { PasienId = RealOrNull(r.Pasien.PasienId) };
            }
        }

        return new AdmisiRajalOfficerWorklistItem(queue, identity, booking, registration);
    }

    private static string? LatestEventReff(PasienTrackerModel tracker, string eventName) =>
        tracker.ListEvent
            .Where(e => string.Equals(e.EventName, eventName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.NoUrut)
            .Select(e => e.ReffId)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id) && id != "-");

    private static AdmisiRajalOfficerWorklistBooking ToBooking(BookingModel b) => new(
        b.BookingId,
        b.TglBerobat.ToString("yyyy-MM-dd"),
        b.JamPraktek.ToString("HH:mm"),
        b.Layanan.LayananId,
        b.Layanan.LayananName,
        b.Dokter.PpaId,
        b.Dokter.PpaName,
        RealOrNull(b.PasienId),
        EmptyToNull(b.CoverageInfo.AsuransiName),
        EmptyToNull(b.CoverageInfo.NoPeserta));

    private static AdmisiRajalOfficerWorklistRegistration ToRegistration(RegModel r) => new(
        r.RegId,
        r.RegDate.ToString("yyyy-MM-dd"),
        r.Pasien.PasienId,
        r.Pasien.PasienName,
        r.Layanan.LayananId,
        r.Layanan.LayananName,
        r.Dokter.PpaId,
        r.Dokter.PpaName);

    private static string? RealOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == "-" ? null : value.Trim();

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Bucket(int count) => count switch
    {
        0 => "0",
        <= 10 => "1-10",
        <= 50 => "11-50",
        <= 100 => "51-100",
        _ => "101-500"
    };
}
