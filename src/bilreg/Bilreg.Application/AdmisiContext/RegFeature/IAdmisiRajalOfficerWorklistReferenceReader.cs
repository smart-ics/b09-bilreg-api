namespace Bilreg.Application.AdmisiContext.RegFeature;

public sealed record AdmisiRajalOfficerWorklistEntryKey(
    string AntrianId,
    int NoUrut);

public sealed record AdmisiRajalOfficerWorklistReferences(
    string? BookingId,
    string? RegistrationId);

public sealed record AdmisiRajalOfficerWorklistReferenceView(
    string AntrianId,
    int NoUrut,
    AdmisiRajalOfficerWorklistReferences References);

public interface IAdmisiRajalOfficerWorklistReferenceReader
{
    IReadOnlyList<AdmisiRajalOfficerWorklistReferenceView> List(
        IReadOnlyCollection<AdmisiRajalOfficerWorklistEntryKey> entries);
}
