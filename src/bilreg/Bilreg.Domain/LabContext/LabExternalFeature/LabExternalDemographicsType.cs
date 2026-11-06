namespace Bilreg.Domain.LabContext.LabExternalFeature;

public record LabExternalDemographicsType(
    string PatientName,
    string Nik,
    DateTime BirthDate,
    string Gender,
    string Phone,
    string Address)
{
    public static LabExternalDemographicsType Default => new(
        string.Empty,
        string.Empty,
        new DateTime(3000, 1, 1),
        string.Empty,
        string.Empty,
        string.Empty);

    public int CalculateAge(DateTime? referenceDate = null)
    {
        if (BirthDate == default || BirthDate.Date == new DateTime(3000, 1, 1))
            return 0;

        var refDate = referenceDate ?? DateTime.Today;
        var age = refDate.Year - BirthDate.Year;
        if (BirthDate.Date > refDate.AddYears(-age)) age--;
        return Math.Max(0, age);
    }
}
