namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IRegistrationHistoryReader
{
    RegistrationSearchView? GetById(string registrationId);

    IReadOnlyList<RegistrationSearchView> FindByPatientIds(
        DateOnly admissionDate,
        IReadOnlyCollection<string> patientIds);

    IReadOnlyList<RegistrationSearchView> FindByRegistrationIds(
        IReadOnlyCollection<string> registrationIds);
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
