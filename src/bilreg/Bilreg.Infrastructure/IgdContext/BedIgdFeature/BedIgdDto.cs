using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.IgdContext.BedIgdFeature;

public record BedIgdDto(
    string BedIgdId,
    string BedIgdName,
    string KamarName,
    string BedState,
    string CurrentIgdVisitId,
    string OccupyUser,
    DateTime OccupyDateTime,
    string CrtUser, DateTime CrtDate,
    string UpdUser, DateTime UpdDate,
    string VodUser, DateTime VodDate)
{
    public static BedIgdDto FromModel(BedIgdModel model)
        => new(
            BedIgdId: model.BedIgdId,
            BedIgdName: model.BedIgdName,
            KamarName: model.KamarName,
            BedState: model.BedState.ToCode(),
            CurrentIgdVisitId: model.CurrentIgdVisitId == "-" ? "" : model.CurrentIgdVisitId,
            OccupyUser: model.OccupyAudit.UserId,
            OccupyDateTime: model.OccupyAudit.Timestamp,
            CrtUser: model.AuditTrail.Created.UserId, CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId, UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId, VodDate: model.AuditTrail.Voided.Timestamp);

    public BedIgdModel ToModel()
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        if (VodUser.Length > 0 && VodDate != new DateTime(3000, 1, 1))
            auditTrail.Batal(VodUser, VodDate);

        var currentVisit = string.IsNullOrEmpty(CurrentIgdVisitId) ? "-" : CurrentIgdVisitId;
        return new BedIgdModel(
            bedIgdId: BedIgdId,
            bedIgdName: BedIgdName,
            kamarName: KamarName,
            bedState: BedState.ToBedStateEnum(),
            currentIgdVisitId: currentVisit,
            occupyAudit: new AuditInfoType(OccupyUser, OccupyDateTime),
            auditTrail: auditTrail);
    }

    public BedIgdView ToView()
        => new(
            BedIgdId: BedIgdId,
            BedIgdName: BedIgdName,
            KamarName: KamarName,
            BedState: BedState,
            CurrentIgdVisitId: CurrentIgdVisitId,
            OccupyDateTime: OccupyDateTime);
}
