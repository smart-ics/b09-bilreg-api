using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public record LabAntibioticDto(
    string AntibioticId,
    string AntibioticName,
    string DiskConcentration,
    string ResistenRange,
    string IntermediateRange,
    string SensitifRange,
    int DisplayOrder,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static LabAntibioticDto FromModel(LabAntibioticModel model) =>
        new(
            AntibioticId: model.AntibioticId,
            AntibioticName: model.AntibioticName,
            DiskConcentration: model.DiskConcentration,
            ResistenRange: model.ResistenRange,
            IntermediateRange: model.IntermediateRange,
            SensitifRange: model.SensitifRange,
            DisplayOrder: model.DisplayOrder,
            CrtUser: model.AuditTrail.Created.UserId,
            CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId,
            UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId,
            VodDate: model.AuditTrail.Voided.Timestamp);

    public LabAntibioticModel ToModel()
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));

        return new LabAntibioticModel(
            antibioticId: AntibioticId,
            antibioticName: AntibioticName,
            diskConcentration: DiskConcentration,
            resistenRange: ResistenRange,
            intermediateRange: IntermediateRange,
            sensitifRange: SensitifRange,
            displayOrder: DisplayOrder,
            auditTrail: auditTrail);
    }
}
