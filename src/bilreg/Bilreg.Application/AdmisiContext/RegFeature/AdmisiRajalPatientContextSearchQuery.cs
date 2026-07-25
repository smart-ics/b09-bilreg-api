using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public sealed record AdmisiRajalPatientContextCandidate(
    string Kind, string Id, string PatientId, string PatientName, string? RegistrationId,
    string? BookingId, string? VisitDate, string? ServiceName, string? Status, string MatchType,
    bool ExactMatch);

public sealed record AdmisiRajalPatientContextSearchResponse(
    string Query, string BusinessDate, IReadOnlyList<AdmisiRajalPatientContextCandidate> Bookings,
    IReadOnlyList<AdmisiRajalPatientContextCandidate> Registrations,
    IReadOnlyList<AdmisiRajalPatientContextCandidate> Patients, bool HasMore);

public sealed record AdmisiRajalPatientContextSearchQuery(string Keyword, string BusinessDate,
    int LimitPerType = 10) : IRequest<AdmisiRajalPatientContextSearchResponse>;

public sealed class AdmisiRajalPatientContextSearchHandler : IRequestHandler<AdmisiRajalPatientContextSearchQuery,
    AdmisiRajalPatientContextSearchResponse>
{
    private readonly IPasienRepo _patients;
    private readonly IBookingRepo _bookings;
    private readonly IRegAktifRepo _activeRegistrations;

    public AdmisiRajalPatientContextSearchHandler(IPasienRepo patients, IBookingRepo bookings,
        IRegAktifRepo activeRegistrations)
    { _patients=patients; _bookings=bookings; _activeRegistrations=activeRegistrations; }

    public Task<AdmisiRajalPatientContextSearchResponse> Handle(AdmisiRajalPatientContextSearchQuery request,
        CancellationToken cancellationToken)
    {
        var keyword=request.Keyword.Trim();
        if (keyword.Length < 3) throw new ArgumentException("Keyword must contain at least three characters.");
        var date=DateOnly.ParseExact(request.BusinessDate, "yyyy-MM-dd");
        var limit=Math.Clamp(request.LimitPerType, 1, 25);
        var patients=(_patients.SearchPasien(keyword) ?? []).Take(limit + 1).Select(x =>
            new AdmisiRajalPatientContextCandidate("patient", x.PasienId, x.PasienId, x.Person.PersonName,
                null, null, null, null, x.IsActive ? "Active" : "Inactive", Match(keyword, x.PasienId, x.Person.PersonName),
                Exact(keyword, x.PasienId))).ToList();
        var bookings=(_bookings.ListDataExtApp(new Periode(date.ToDateTime(TimeOnly.MinValue))) ?? []).Where(x =>
                x.BookingId.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Reg.PasienId.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.Person.PersonName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Take(limit + 1).Select(x => new AdmisiRajalPatientContextCandidate("booking", x.BookingId,
                x.Reg.PasienId, x.Person.PersonName, x.Reg.RegId == "-" ? null : x.Reg.RegId, x.BookingId,
                x.TglBerobat.ToString("yyyy-MM-dd"), x.Layanan.LayananName, x.Reg.RegId == "-" ? "Ready" : "Registered",
                Match(keyword, x.BookingId, x.Person.PersonName), Exact(keyword, x.BookingId))).ToList();
        var regs=(_activeRegistrations.ListData(keyword) ?? []).Take(limit + 1).Select(x =>
            new AdmisiRajalPatientContextCandidate("registration", x.RegId, x.PasienId, x.PasienName, x.RegId,
                null, x.RegDate, x.LayananName, "Active", Match(keyword, x.RegId, x.PasienName), Exact(keyword, x.RegId))).ToList();
        return Task.FromResult(new AdmisiRajalPatientContextSearchResponse(keyword, request.BusinessDate,
            bookings.Take(limit).OrderByDescending(x => x.ExactMatch).ToList(),
            regs.Take(limit).OrderByDescending(x => x.ExactMatch).ToList(),
            patients.Take(limit).OrderByDescending(x => x.ExactMatch).ToList(),
            bookings.Count > limit || regs.Count > limit || patients.Count > limit));
    }

    private static bool Exact(string keyword, string id) => id.Equals(keyword, StringComparison.OrdinalIgnoreCase);
    private static string Match(string keyword, string id, string name) => Exact(keyword,id) ? "ExactIdentifier" :
        name.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) ? "PatientNamePrefix" : "PatientNameContains";
}
