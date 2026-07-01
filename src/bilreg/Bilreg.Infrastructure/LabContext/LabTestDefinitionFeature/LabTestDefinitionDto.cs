using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionDto(
    string TestDefinitionId,
    string TarifId,
    string TarifCode,
    string TarifName,
    string LabTestCode,
    string LabTestName,
    string SpecimenType,
    int VacutainerType,
    bool IsActive,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static LabTestDefinitionDto FromModel(LabTestDefinitionModel model) =>
        new(
            model.TestDefinitionId,
            model.TarifId,
            model.TarifCode,
            model.TarifName,
            model.LabTestCode,
            model.LabTestName,
            model.SpecimenType,
            (int)model.VacutainerType,
            model.IsActive,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId.Length > 0
                ? model.AuditTrail.Modified.UserId
                : model.AuditTrail.Created.UserId,
            model.AuditTrail.Modified.Timestamp != EmptyDate
                ? model.AuditTrail.Modified.Timestamp
                : model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);

    public LabTestDefinitionModel ToModel(IEnumerable<LabTestComponentModel> components) =>
        new(
            TestDefinitionId,
            TarifId,
            TarifCode,
            TarifName,
            LabTestCode,
            LabTestName,
            SpecimenType,
            (VacutainerTypeEnum)VacutainerType,
            IsActive,
            new AuditTrailType(
                new AuditInfoType(CrtUser, CrtDate),
                new AuditInfoType(UpdUser, UpdDate),
                new AuditInfoType(VodUser, VodDate)),
            components);
}
