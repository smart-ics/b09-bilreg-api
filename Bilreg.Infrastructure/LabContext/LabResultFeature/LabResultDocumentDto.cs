using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public record LabResultDocumentDto(
    string ResultDocumentId,
    string OrderId,
    int VersionNo,
    bool IsCurrentVersion,
    int ResultSource,
    int ResultStatus,
    DateTime RecordedDate,
    string RecordedUserId,
    DateTime VerifiedDate,
    string VerifiedUserId,
    string AmendmentReason,
    DateTime AmendedDate,
    string AmendedUserId,
    string PreviousVersionId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static LabResultDocumentDto FromModel(LabResultDocumentModel model)
        => new(
            ResultDocumentId: model.ResultDocumentId,
            OrderId: model.OrderId,
            VersionNo: model.VersionNo,
            IsCurrentVersion: model.IsCurrentVersion,
            ResultSource: (int)model.ResultSource,
            ResultStatus: (int)model.ResultStatus,
            RecordedDate: model.RecordedDate,
            RecordedUserId: model.RecordedUserId,
            VerifiedDate: model.VerifiedDate,
            VerifiedUserId: model.VerifiedUserId,
            AmendmentReason: model.AmendmentReason,
            AmendedDate: model.AmendedDate,
            AmendedUserId: model.AmendedUserId,
            PreviousVersionId: model.PreviousVersionId,
            CrtUser: model.AuditTrail.Created.UserId,
            CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId,
            UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId,
            VodDate: model.AuditTrail.Voided.Timestamp);

    public LabResultDocumentModel ToModel(IEnumerable<LabResultItemModel> items)
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        if (VodUser.Length > 0 && VodDate != new DateTime(3000, 1, 1))
            auditTrail.Batal(VodUser, VodDate);

        return LabResultDocumentModel.Load(
            ResultDocumentId,
            OrderId,
            VersionNo,
            IsCurrentVersion,
            (LabResultSourceEnum)ResultSource,
            (LabResultStatusEnum)ResultStatus,
            RecordedDate,
            RecordedUserId,
            VerifiedDate,
            VerifiedUserId,
            AmendmentReason,
            AmendedDate,
            AmendedUserId,
            PreviousVersionId,
            auditTrail,
            items);
    }
}
