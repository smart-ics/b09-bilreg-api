namespace Bilreg.Infrastructure.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionViewDto(
    string TestDefinitionId,
    string TarifId,
    string TarifCode,
    string TarifName,
    string LabTestCode,
    string LabTestName,
    string SpecimenType,
    int VacutainerType,
    bool IsActive,
    int ComponentCount,
    decimal TarifPrice)
{
    public LabTestDefinitionViewDto() : this(
        string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty, 1, false, 0, 0)
    {
    }
}
