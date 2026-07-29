namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IRegistrationHistoryReader
{
    RegistrationSearchView? GetById(string registrationId);

    IReadOnlyList<RegistrationSearchView> Search(string keyword, DateOnly admissionDate);

    IReadOnlyList<RegistrationSearchView> FindRelated(
        DateOnly admissionDate,
        IReadOnlyCollection<string> registrationIds,
        IReadOnlyCollection<string> patientIds);
}

public sealed record RegistrationSearchView(
    string RegistrationId,
    DateOnly AdmissionDate,
    TimeOnly? AdmissionTime,
    string PatientId,
    string PatientName,
    DateOnly? BirthDate,
    string? Gender,
    string? ServiceId,
    string? ServiceName,
    string? DoctorId,
    string? DoctorName,
    string? GuaranteeName,
    DateOnly? ExitDate,
    DateOnly? VoidDate);
