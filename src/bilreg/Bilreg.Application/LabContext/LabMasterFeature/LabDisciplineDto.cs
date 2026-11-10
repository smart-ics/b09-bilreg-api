using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public record LabDisciplineDto(
    string DisciplineId,
    string DisciplineName,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static LabDisciplineDto FromModel(LabDisciplineModel model) =>
        new(
            DisciplineId: model.DisciplineId,
            DisciplineName: model.DisciplineName,
            CrtUser: model.AuditTrail.Created.UserId,
            CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId,
            UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId,
            VodDate: model.AuditTrail.Voided.Timestamp);

    public LabDisciplineModel ToModel()
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));

        return new LabDisciplineModel(
            disciplineId: DisciplineId,
            disciplineName: DisciplineName,
            auditTrail: auditTrail);
    }
}
