using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public record LabTestGroupDto(
    string TestGroupId,
    string TestGroupName,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static LabTestGroupDto FromModel(LabTestGroupModel model) =>
        new(
            TestGroupId: model.TestGroupId,
            TestGroupName: model.TestGroupName,
            CrtUser: model.AuditTrail.Created.UserId,
            CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId,
            UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId,
            VodDate: model.AuditTrail.Voided.Timestamp);

    public LabTestGroupModel ToModel()
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));

        return new LabTestGroupModel(
            testGroupId: TestGroupId,
            testGroupName: TestGroupName,
            auditTrail: auditTrail);
    }
}
