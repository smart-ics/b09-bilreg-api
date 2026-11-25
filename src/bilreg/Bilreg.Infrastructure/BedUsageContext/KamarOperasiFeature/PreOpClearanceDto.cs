using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record PreOpClearanceDto(
    string ClearanceId,
    string ScheduleOpId,
    string OrderOpId,
    string RegId,
    int NoUrut,
    int ClearanceStatus,
    string VerifikatorPpaId,
    string DokterPjId,
    DateTime CompletedAt,
    string DokterOverrideId,
    string AlasanOverride,
    string AlasanNotCleared,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static PreOpClearanceDto FromModel(PreOpClearanceModel model)
    {
        return new PreOpClearanceDto(
            ClearanceId: model.ClearanceId,
            ScheduleOpId: model.ScheduleOpId,
            OrderOpId: model.OrderOpId,
            RegId: model.RegId,
            NoUrut: model.NoUrut,
            ClearanceStatus: (int)model.ClearanceStatus,
            VerifikatorPpaId: model.VerifikatorPpaId,
            DokterPjId: model.DokterPjId,
            CompletedAt: model.CompletedAt,
            DokterOverrideId: model.DokterOverrideId,
            AlasanOverride: model.AlasanOverride,
            AlasanNotCleared: model.AlasanNotCleared,
            CrtUser: model.AuditTrail.Created.UserId,
            CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId,
            UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId,
            VodDate: model.AuditTrail.Voided.Timestamp
        );
    }

    public PreOpClearanceModel ToModel(IEnumerable<PreOpChecklistItemType> checklistItems)
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate)
        );

        return new PreOpClearanceModel(
            ClearanceId,
            ScheduleOpId,
            OrderOpId,
            RegId,
            NoUrut,
            (PreOpClearanceStatusEnum)ClearanceStatus,
            checklistItems,
            VerifikatorPpaId,
            DokterPjId,
            CompletedAt,
            DokterOverrideId,
            AlasanOverride,
            AlasanNotCleared,
            auditTrail
        );
    }
}
