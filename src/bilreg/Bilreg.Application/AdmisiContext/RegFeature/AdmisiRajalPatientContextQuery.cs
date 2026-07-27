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
    private static readonly Regex CoreIdPattern = new(
        @"^(BO|RG)[A-Z0-9-]+$|^\d{6,12}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DemographicPattern = new(
        @"^.{3,}\s*\|\s*\d{4}-\d{2}-\d{2}\s*\|\s*\+?\d{8,15}$",
        RegexOptions.Compiled);

    private readonly IBookingRepo _bookingRepo;
    private readonly IRegAktifRepo _regRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly ILogger<AdmisiRajalPatientContextHandler> _logger;

    public AdmisiRajalPatientContextHandler(
        IBookingRepo bookingRepo,
        IRegAktifRepo regRepo,
        IPasienRepo pasienRepo,
        ILogger<AdmisiRajalPatientContextHandler>? logger = null)
    {
        _bookingRepo = bookingRepo;
        _regRepo = regRepo;
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

        var bookings = Includes(request.Scope, PatientContextKind.Booking)
            ? SearchBookings(request, keyword, businessDate)
            : [];
        var registrations = Includes(request.Scope, PatientContextKind.Registration)
            ? SearchRegistrations(request, keyword, businessDate)
            : [];
        var patients = Includes(request.Scope, PatientContextKind.Patient)
            ? SearchPatients(request, keyword)
            : [];

        var bookingGroup = ToGroup(bookings, request.LimitPerType);
        var registrationGroup = ToGroup(registrations, request.LimitPerType);
        var patientGroup = ToGroup(patients, request.LimitPerType);
        var bestMatch = bookings.Concat(registrations).Concat(patients)
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

    private List<AdmisiRajalPatientContextResult> SearchBookings(
        AdmisiRajalPatientContextSearchQuery request,
        string keyword,
        DateOnly businessDate)
    {
        var period = new Periode(businessDate.ToDateTime(TimeOnly.MinValue));
        return _bookingRepo.ListDataTglBerobat(period)
            .Where(x => BookingMatches(x, keyword))
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

    private List<AdmisiRajalPatientContextResult> SearchRegistrations(
        AdmisiRajalPatientContextSearchQuery request,
        string keyword,
        DateOnly businessDate) =>
        _regRepo.ListData(keyword)
            .Where(x => string.Equals(x.RegDate, businessDate.ToString("yyyy-MM-dd"), StringComparison.Ordinal))
            .Select(x => ToRegistrationResult(
                x,
                keyword,
                string.Equals(x.RegId, request.SuggestedRegistrationId, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Rank)
            .ThenByDescending(x => x.VisitDate)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private List<AdmisiRajalPatientContextResult> SearchPatients(
        AdmisiRajalPatientContextSearchQuery request,
        string keyword)
    {
        IEnumerable<PasienPersonView> data;
        if (NikPattern.IsMatch(keyword))
        {
            var patient = _pasienRepo.GetDataByNik(keyword);
            data = patient.HasValue ? [patient.Value] : [];
        }
        else
        {
            data = _pasienRepo.SearchPasien(keyword);
        }
        return data
            .Select(x => ToPatientResult(
                x,
                keyword,
                string.Equals(x.PasienId, request.SuggestedPatientId, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.PatientName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private AdmisiRajalPatientContextResult? GetBooking(string id, DateOnly businessDate)
    {
        var maybe = _bookingRepo.LoadEntity(BookingModel.Key(id));
        if (!maybe.HasValue || maybe.Value.TglBerobat != businessDate) return null;
        return ToBookingResult(maybe.Value.ToSearchView(), businessDate, id, false);
    }

    private AdmisiRajalPatientContextResult? GetRegistration(string id, DateOnly businessDate)
    {
        var maybe = _regRepo.LoadEntity(RegModel.Key(id));
        if (!maybe.HasValue || maybe.Value.RegDate != businessDate) return null;
        var reg = maybe.Value;
        var view = new RegSearchRegView(
            reg.RegId,
            reg.RegDate.ToString("yyyy-MM-dd"),
            reg.Pasien.PasienId,
            reg.Pasien.PasienName,
            reg.TipeJaminan.TipeJaminanName,
            reg.Layanan.LayananName,
            ((int)reg.JenisReg).ToString(),
            reg.JenisReg.ToString());
        return ToRegistrationResult(view, id, false);
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
        RegSearchRegView reg,
        string keyword,
        bool suggested)
    {
        var exact = EqualsNormalized(reg.RegId, keyword) || EqualsNormalized(reg.PasienId, keyword);
        return new AdmisiRajalPatientContextResult(
            PatientContextKind.Registration,
            reg.RegId,
            reg.PasienName,
            reg.PasienId,
            null,
            null,
            null,
            null,
            null,
            reg.RegDate,
            null,
            reg.LayananName,
            null,
            "Active",
            null,
            reg.RegId,
            exact ? "ExactIdentifier" : "PatientName",
            exact,
            Rank(exact, suggested, true, false, reg.PasienName, keyword, false),
            []);
    }

    private static AdmisiRajalPatientContextResult ToPatientResult(
        PasienPersonView patient,
        string keyword,
        bool suggested,
        string? nik = null)
    {
        var exact = EqualsNormalized(patient.PasienId, keyword) || EqualsNormalized(nik, keyword);
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
            exact ? "ExactIdentifier" : "PatientName",
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
