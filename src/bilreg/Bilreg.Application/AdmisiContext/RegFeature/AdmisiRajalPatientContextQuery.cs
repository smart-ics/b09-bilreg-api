using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PatientContextKind
{
    Booking,
    Registration,
    Patient
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PatientContextScope
{
    All,
    Booking,
    Registration,
    Patient
}

public sealed record AdmisiRajalPatientContextSearchQuery(
    string Keyword,
    string BusinessDate,
    PatientContextScope Scope = PatientContextScope.All,
    int LimitPerType = 10,
    string? SuggestedBookingId = null,
    string? SuggestedRegistrationId = null,
    string? SuggestedPatientId = null)
    : IRequest<AdmisiRajalPatientContextSearchResponse>;

public sealed record AdmisiRajalPatientContextGetQuery(
    PatientContextKind Kind,
    string Id,
    string BusinessDate)
    : IRequest<AdmisiRajalPatientContextResult>;

public sealed record AdmisiRajalPatientContextResult(
    PatientContextKind Kind,
    string Id,
    string PatientName,
    string? PatientId,
    string? BirthDate,
    string? Gender,
    string? Locality,
    string? MaskedNik,
    string? MaskedPhone,
    string? VisitDate,
    string? VisitTime,
    string? ServiceName,
    string? DoctorName,
    string State,
    string? BookingId,
    string? RegistrationId,
    string MatchType,
    bool IsExactMatch,
    int Rank,
    IReadOnlyList<string> Warnings);

public sealed record AdmisiRajalPatientContextGroup(
    IReadOnlyList<AdmisiRajalPatientContextResult> Items,
    int Total,
    bool HasMore);

public sealed record AdmisiRajalPatientContextSearchResponse(
    string BusinessDate,
    AdmisiRajalPatientContextGroup Bookings,
    AdmisiRajalPatientContextGroup Registrations,
    AdmisiRajalPatientContextGroup Patients,
    AdmisiRajalPatientContextResult? BestMatch,
    bool CanCreatePatient);

public sealed class AdmisiRajalPatientContextHandler :
    IRequestHandler<AdmisiRajalPatientContextSearchQuery, AdmisiRajalPatientContextSearchResponse>,
    IRequestHandler<AdmisiRajalPatientContextGetQuery, AdmisiRajalPatientContextResult>
{
    private static readonly Meter Meter = new("Bilreg.AdmisiRajal", "1.0");
    private static readonly Histogram<double> SearchDuration = Meter.CreateHistogram<double>(
        "bilreg.admisi_rajal.patient_context_search.duration",
        "ms");
    private static readonly Counter<long> SearchRequests = Meter.CreateCounter<long>(
        "bilreg.admisi_rajal.patient_context_search.requests");
    private static readonly Regex NikPattern = new(@"^\d{16}$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^\+?\d{8,15}$", RegexOptions.Compiled);
    private static readonly Regex FullRegistrationIdPattern = new(
        @"^RG\d{8}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CoreIdPattern = new(
        @"^(BO|RG)[A-Z0-9-]+$|^\d{6,12}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DemographicPattern = new(
        @"^.{3,}\s*\|\s*\d{4}-\d{2}-\d{2}\s*\|\s*\+?\d{8,15}$",
        RegexOptions.Compiled);

    private readonly IBookingRepo _bookingRepo;
    private readonly IRegistrationHistoryReader _registrationReader;
    private readonly IPasienRepo _pasienRepo;
    private readonly ILogger<AdmisiRajalPatientContextHandler> _logger;

    public AdmisiRajalPatientContextHandler(
        IBookingRepo bookingRepo,
        IRegistrationHistoryReader registrationReader,
        IPasienRepo pasienRepo,
        ILogger<AdmisiRajalPatientContextHandler>? logger = null)
    {
        _bookingRepo = bookingRepo;
        _registrationReader = registrationReader;
        _pasienRepo = pasienRepo;
        _logger = logger ?? NullLogger<AdmisiRajalPatientContextHandler>.Instance;
    }

    public Task<AdmisiRajalPatientContextSearchResponse> Handle(
        AdmisiRajalPatientContextSearchQuery request,
        CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        var keyword = NormalizeKeyword(request.Keyword);
        var businessDate = ParseDate(request.BusinessDate);
        ValidateSearch(keyword, request.LimitPerType);

        var (bookings, registrations, patients) = FullRegistrationIdPattern.IsMatch(keyword)
            ? SearchExactRegistration(request, keyword)
            : SearchDateBounded(request, keyword, businessDate);

        var bookingGroup = ToGroup(bookings, request.LimitPerType);
        var registrationGroup = ToGroup(registrations, request.LimitPerType);
        var patientGroup = ToGroup(patients, request.LimitPerType);
        var bestMatch = registrationGroup.Items.Concat(bookingGroup.Items).Concat(patientGroup.Items)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Kind)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        var canCreatePatient = (NikPattern.IsMatch(keyword) || DemographicPattern.IsMatch(keyword))
            && patientGroup.Total == 0;

        watch.Stop();
        var exact = IsExactIdentifier(keyword);
        SearchDuration.Record(
            watch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", request.Scope.ToString()),
            new KeyValuePair<string, object?>("exact", exact),
            new KeyValuePair<string, object?>("partial", false));
        SearchRequests.Add(
            1,
            new KeyValuePair<string, object?>("scope", request.Scope.ToString()),
            new KeyValuePair<string, object?>("exact", exact),
            new KeyValuePair<string, object?>("partial", false));
        _logger.LogInformation(
            "Admisi Rajal patient-context search scope={Scope} exact={Exact} bookingCount={BookingCount} registrationCount={RegistrationCount} patientCount={PatientCount} partial={Partial} elapsedMs={ElapsedMs}",
            request.Scope,
            exact,
            bookingGroup.Total,
            registrationGroup.Total,
            patientGroup.Total,
            false,
            watch.ElapsedMilliseconds);

        return Task.FromResult(new AdmisiRajalPatientContextSearchResponse(
            request.BusinessDate,
            bookingGroup,
            registrationGroup,
            patientGroup,
            bestMatch,
            canCreatePatient));
    }

    public Task<AdmisiRajalPatientContextResult> Handle(
        AdmisiRajalPatientContextGetQuery request,
        CancellationToken cancellationToken)
    {
        var businessDate = ParseDate(request.BusinessDate);
        var id = NormalizeKeyword(request.Id);
        var result = request.Kind switch
        {
            PatientContextKind.Booking => GetBooking(id, businessDate),
            PatientContextKind.Registration => GetRegistration(id, businessDate),
            PatientContextKind.Patient => GetPatient(id),
            _ => null
        };
        return Task.FromResult(result
            ?? throw new KeyNotFoundException($"{request.Kind} context '{id}' not found."));
    }

    private (
        List<AdmisiRajalPatientContextResult> Bookings,
        List<AdmisiRajalPatientContextResult> Registrations,
        List<AdmisiRajalPatientContextResult> Patients)
        SearchExactRegistration(AdmisiRajalPatientContextSearchQuery request, string keyword)
    {
        var registration = _registrationReader.GetById(keyword);
        if (registration is null) return ([], [], []);

        var registrationResult = ToRegistrationResult(
            registration,
            keyword,
            string.Equals(
                registration.RegistrationId,
                request.SuggestedRegistrationId,
                StringComparison.OrdinalIgnoreCase),
            false);
        var registrations = request.Scope == PatientContextScope.Patient
            ? new List<AdmisiRajalPatientContextResult>()
            : [registrationResult];
        return (
            [],
            registrations,
            LoadPatients([registration.PatientId], keyword, request.SuggestedPatientId));
    }

    private (
        List<AdmisiRajalPatientContextResult> Bookings,
        List<AdmisiRajalPatientContextResult> Registrations,
        List<AdmisiRajalPatientContextResult> Patients)
        SearchDateBounded(
            AdmisiRajalPatientContextSearchQuery request,
            string keyword,
            DateOnly businessDate)
    {
        var bookingViews = Includes(request.Scope, PatientContextKind.Booking)
            ? SearchBookingViews(keyword, businessDate)
            : [];
        var directPatients = SearchPatients(request, keyword);
        var registrationIds = bookingViews
            .Select(x => Real(x.Reg.RegId))
            .Where(x => x is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var bookingPatientIds = bookingViews
            .Select(x => Real(x.Reg.PasienId))
            .Where(x => x is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var directPatientIds = directPatients
            .Select(x => Real(x.PatientId))
            .Where(x => x is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var relatedPatientIds = request.Scope switch
        {
            PatientContextScope.All => bookingPatientIds
                .Concat(directPatientIds)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            PatientContextScope.Booking => bookingPatientIds,
            PatientContextScope.Registration => directPatientIds,
            _ => []
        };
        var registrationViews = SearchRegistrationViews(
            request,
            keyword,
            businessDate,
            registrationIds,
            relatedPatientIds);
        var registrations = registrationViews
            .Select(x => ToRegistrationResult(
                x,
                keyword,
                string.Equals(
                    x.RegistrationId,
                    request.SuggestedRegistrationId,
                    StringComparison.OrdinalIgnoreCase),
                bookingViews.Any(booking => IsSuccessor(booking, x))))
            .OrderBy(x => x.Rank)
            .ThenByDescending(x => x.VisitDate)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var bookings = bookingViews
            .Where(x => !registrationViews.Any(registration => IsSuccessor(x, registration)))
            .Select(x => ToBookingResult(
                x,
                businessDate,
                keyword,
                string.Equals(
                    x.BookingId,
                    request.SuggestedBookingId,
                    StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var impliedPatientIds = bookingViews.Select(x => x.Reg.PasienId)
            .Concat(registrationViews.Select(x => x.PatientId));
        var patients = directPatients
            .Concat(LoadPatients(impliedPatientIds, keyword, request.SuggestedPatientId))
            .DistinctBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return (bookings, registrations, patients);
    }

    private List<BookingView> SearchBookingViews(string keyword, DateOnly businessDate)
    {
        var period = new Periode(businessDate.ToDateTime(TimeOnly.MinValue));
        return _bookingRepo.ListDataTglBerobat(period)
            .Where(x => BookingMatches(x, keyword))
            .DistinctBy(x => x.BookingId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<AdmisiRajalPatientContextResult> SearchBookings(
        AdmisiRajalPatientContextSearchQuery request,
        string keyword,
        DateOnly businessDate)
    {
        return SearchBookingViews(keyword, businessDate)
            .Select(x => ToBookingResult(
                x,
                businessDate,
                keyword,
                string.Equals(x.BookingId, request.SuggestedBookingId, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Rank)
            .ThenByDescending(x => x.VisitDate)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<RegistrationSearchView> SearchRegistrationViews(
        AdmisiRajalPatientContextSearchQuery request,
        string keyword,
        DateOnly businessDate,
        IReadOnlyCollection<string> registrationIds,
        IReadOnlyCollection<string> patientIds)
    {
        var direct = Includes(request.Scope, PatientContextKind.Registration)
            ? _registrationReader.Search(keyword, businessDate) ?? []
            : [];
        var related = request.Scope == PatientContextScope.Patient
            || (registrationIds.Count == 0 && patientIds.Count == 0)
            ? []
            : _registrationReader.FindRelated(businessDate, registrationIds, patientIds) ?? [];
        return direct.Concat(related)
            .DistinctBy(x => x.RegistrationId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<AdmisiRajalPatientContextResult> LoadPatients(
        IEnumerable<string> patientIds,
        string keyword,
        string? suggestedPatientId) =>
        patientIds
            .Where(x => Real(x) is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id => _pasienRepo.LoadEntity(PasienModel.Key(id)))
            .Where(x => x.HasValue)
            .Select(x =>
            {
                var patient = x.Value;
                return ToPatientResult(
                    new PasienPersonView(patient.PasienId, patient.IsAktif, patient.Person),
                    keyword,
                    string.Equals(
                        patient.PasienId,
                        suggestedPatientId,
                        StringComparison.OrdinalIgnoreCase),
                    patient.Ktp.Nik);
            })
            .ToList();

    private static bool IsSuccessor(BookingView booking, RegistrationSearchView registration)
    {
        var linkedRegistrationId = Real(booking.Reg.RegId);
        if (linkedRegistrationId is not null)
            return EqualsNormalized(registration.RegistrationId, linkedRegistrationId);

        if (!EqualsNormalized(registration.PatientId, booking.Reg.PasienId)
            || registration.AdmissionDate != booking.TglBerobat)
            return false;

        var bookingServiceId = Real(booking.Layanan.LayananId);
        var registrationServiceId = Real(registration.ServiceId);
        return bookingServiceId is null
            || registrationServiceId is null
            || EqualsNormalized(registrationServiceId, bookingServiceId);
    }

    private List<AdmisiRajalPatientContextResult> SearchPatients(
        AdmisiRajalPatientContextSearchQuery request,
        string keyword)
    {
        IEnumerable<PasienPersonView> directData;
        if (NikPattern.IsMatch(keyword))
        {
            var patient = _pasienRepo.GetDataByNik(keyword);
            directData = patient.HasValue ? [patient.Value] : [];
        }
        else
        {
            directData = _pasienRepo.SearchPasien(keyword);
        }
        var phoneData = PhonePattern.IsMatch(keyword)
            ? _pasienRepo.SearchPasienByPhone(keyword)
            : [];
        var phonePatientIds = phoneData
            .Select(x => x.PasienId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return directData
            .Concat(phoneData)
            .DistinctBy(x => x.PasienId, StringComparer.OrdinalIgnoreCase)
            .Select(x => ToPatientResult(
                x,
                keyword,
                string.Equals(x.PasienId, request.SuggestedPatientId, StringComparison.OrdinalIgnoreCase),
                null,
                phonePatientIds.Contains(x.PasienId)))
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.PatientName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private AdmisiRajalPatientContextResult? GetBooking(string id, DateOnly businessDate)
    {
        var maybe = _bookingRepo.LoadEntity(BookingModel.Key(id));
        if (!maybe.HasValue || maybe.Value.TglBerobat != businessDate) return null;
        var booking = maybe.Value.ToSearchView();
        var related = _registrationReader.FindRelated(
            businessDate,
            Real(booking.Reg.RegId) is { } registrationId ? [registrationId] : [],
            Real(booking.Reg.PasienId) is { } patientId ? [patientId] : []);
        var successor = related.FirstOrDefault(x => IsSuccessor(booking, x));
        return successor is null
            ? ToBookingResult(booking, businessDate, id, false)
            : ToRegistrationResult(successor, id, false, true);
    }

    private AdmisiRajalPatientContextResult? GetRegistration(string id, DateOnly businessDate)
    {
        var registration = _registrationReader.GetById(id);
        return registration is null ? null : ToRegistrationResult(registration, id, false, false);
    }

    private AdmisiRajalPatientContextResult? GetPatient(string id)
    {
        var maybe = _pasienRepo.LoadEntity(PasienModel.Key(id));
        if (!maybe.HasValue) return null;
        var pasien = maybe.Value;
        return ToPatientResult(
            new PasienPersonView(pasien.PasienId, pasien.IsAktif, pasien.Person),
            id,
            false,
            pasien.Ktp.Nik);
    }

    private static bool BookingMatches(BookingView booking, string keyword) =>
        EqualsNormalized(booking.BookingId, keyword)
        || EqualsNormalized(booking.Reg.PasienId, keyword)
        || EqualsNormalized(booking.Reg.RegId, keyword)
        || ContainsNormalized(booking.Person.PersonName, keyword)
        || EqualsNormalized(booking.Person.Contact.ContactDetail, keyword);

    private static AdmisiRajalPatientContextResult ToBookingResult(
        BookingView booking,
        DateOnly businessDate,
        string keyword,
        bool suggested)
    {
        var exact = EqualsNormalized(booking.BookingId, keyword)
            || EqualsNormalized(booking.Reg.PasienId, keyword);
        var hasRegistration = Real(booking.Reg.RegId) is not null;
        var today = booking.TglBerobat == businessDate;
        var rank = Rank(exact, suggested, hasRegistration, today, booking.Person.PersonName, keyword, false);
        var warnings = hasRegistration
            ? new[] { $"Booking already registered as {booking.Reg.RegId}" }
            : [];
        return new AdmisiRajalPatientContextResult(
            PatientContextKind.Booking,
            booking.BookingId,
            booking.Person.PersonName,
            Real(booking.Reg.PasienId),
            booking.Person.TglLahir.ToString("yyyy-MM-dd"),
            booking.Person.Gender,
            booking.Person.Alamat.Kota,
            null,
            Mask(booking.Person.Contact.ContactDetail, 4),
            booking.TglBerobat.ToString("yyyy-MM-dd"),
            booking.JamPraktek.ToString("HH:mm"),
            booking.Layanan.LayananName,
            booking.Dokter.PpaName,
            hasRegistration ? "Registered" : today ? "Ready" : "Historical",
            booking.BookingId,
            Real(booking.Reg.RegId),
            exact ? "ExactIdentifier" : "PatientName",
            exact,
            rank,
            warnings);
    }

    private static AdmisiRajalPatientContextResult ToRegistrationResult(
        RegistrationSearchView reg,
        string keyword,
        bool suggested,
        bool resolvedFromBooking)
    {
        var exact = EqualsNormalized(reg.RegistrationId, keyword)
            || EqualsNormalized(reg.PatientId, keyword);
        var rank = resolvedFromBooking && !exact
            ? 15
            : Rank(exact, suggested, true, false, reg.PatientName, keyword, false);
        return new AdmisiRajalPatientContextResult(
            PatientContextKind.Registration,
            reg.RegistrationId,
            reg.PatientName,
            reg.PatientId,
            reg.BirthDate?.ToString("yyyy-MM-dd"),
            reg.Gender,
            null,
            null,
            null,
            reg.AdmissionDate.ToString("yyyy-MM-dd"),
            reg.AdmissionTime?.ToString("HH:mm"),
            reg.ServiceName,
            reg.DoctorName,
            reg.ExitDate is null ? "Active" : "Discharged",
            null,
            reg.RegistrationId,
            exact ? "ExactIdentifier" : resolvedFromBooking ? "BookingSuccessor" : "PatientName",
            exact,
            rank,
            []);
    }

    private static AdmisiRajalPatientContextResult ToPatientResult(
        PasienPersonView patient,
        string keyword,
        bool suggested,
        string? nik = null,
        bool matchedByPhone = false)
    {
        var exactIdentifier = EqualsNormalized(patient.PasienId, keyword)
            || EqualsNormalized(nik, keyword);
        var exact = exactIdentifier || matchedByPhone;
        return new AdmisiRajalPatientContextResult(
            PatientContextKind.Patient,
            patient.PasienId,
            patient.Person.PersonName,
            patient.PasienId,
            patient.Person.TglLahir.ToString("yyyy-MM-dd"),
            patient.Person.Gender,
            patient.Person.Alamat.Kota,
            Mask(nik ?? patient.Person.Identity.NomorId, 4),
            Mask(patient.Person.Contact.ContactDetail, 4),
            null,
            null,
            null,
            null,
            patient.IsActive ? "Active" : "Inactive",
            null,
            null,
            exactIdentifier ? "ExactIdentifier" : matchedByPhone ? "ExactPhone" : "PatientName",
            exact,
            Rank(exact, suggested, false, false, patient.Person.PersonName, keyword, !patient.IsActive),
            []);
    }

    private static int Rank(
        bool exact,
        bool suggested,
        bool activeRegistration,
        bool currentBooking,
        string name,
        string keyword,
        bool inactive)
    {
        if (exact) return 10;
        if (suggested) return 20;
        if (activeRegistration) return 30;
        if (currentBooking) return 40;
        if (EqualsNormalized(name, keyword)) return 60;
        if (NormalizeKeyword(name).StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return 70;
        return inactive ? 90 : 80;
    }

    private static AdmisiRajalPatientContextGroup ToGroup(
        IReadOnlyList<AdmisiRajalPatientContextResult> items,
        int limit) =>
        new(items.Take(limit).ToList(), items.Count, items.Count > limit);

    private static bool Includes(PatientContextScope scope, PatientContextKind kind) =>
        scope == PatientContextScope.All
        || string.Equals(scope.ToString(), kind.ToString(), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeKeyword(string value) => value.Trim().ToUpperInvariant();

    private static DateOnly ParseDate(string value)
    {
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            throw new ArgumentException("BusinessDate must use yyyy-MM-dd.", nameof(value));
        return date;
    }

    private static void ValidateSearch(string keyword, int limit)
    {
        if (string.IsNullOrWhiteSpace(keyword)) throw new ArgumentException("Keyword is required.");
        if (!IsExactIdentifier(keyword) && keyword.Length < 3)
            throw new ArgumentException("Keyword must contain at least three characters.");
        if (limit is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(limit));
    }

    private static bool IsExactIdentifier(string keyword) =>
        NikPattern.IsMatch(keyword) || CoreIdPattern.IsMatch(keyword);

    private static bool EqualsNormalized(string? value, string keyword) =>
        NormalizeKeyword(value ?? string.Empty) == keyword;

    private static bool ContainsNormalized(string? value, string keyword) =>
        NormalizeKeyword(value ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static string? Real(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == "-" ? null : value.Trim();

    private static string? Mask(string? value, int visibleSuffix)
    {
        var normalized = Real(value);
        if (normalized is null) return null;
        if (normalized.Length <= visibleSuffix) return new string('•', normalized.Length);
        return $"{new string('•', normalized.Length - visibleSuffix)}{normalized[^visibleSuffix..]}";
    }
}

internal static class BookingPatientContextExtensions
{
    public static BookingView ToSearchView(this BookingModel booking) =>
        new(
            booking.BookingId,
            booking.BookingDate,
            booking.Person,
            booking.Reg,
            booking.TglBerobat,
            booking.JamPraktek,
            booking.Layanan,
            booking.Dokter,
            booking.NoAntrian);
}
