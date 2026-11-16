using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public record MutasiBrgDto(
    string MutasiBrgId,
    DateTime MutasiBrgDate,
    MutasiBrgStateEnum State,
    string LayananAsalId,
    string LayananAsalName,
    string LayananTujuanId,
    string LayananTujuanName,
    string OrderMutasiId,
    string DispatchNote,
    string IssuerUserId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static MutasiBrgDto FromModel(MutasiBrgModel model)
    {
        return new MutasiBrgDto(
            model.MutasiBrgId,
            model.MutasiBrgDate,
            model.State,
            model.LayananAsal.LayananId,
            model.LayananAsal.LayananName,
            model.LayananTujuan.LayananId,
            model.LayananTujuan.LayananName,
            model.OrderMutasiId,
            model.DispatchNote,
            model.IssuerUserId,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp
        );
    }

    public MutasiBrgModel ToModel(IEnumerable<MutasiBrgItemModel>? listItem = null)
    {
        var crt = new AuditInfoType(CrtUser, CrtDate);
        var upd = new AuditInfoType(UpdUser, UpdDate);
        var vod = new AuditInfoType(VodUser, VodDate);
        var auditTrail = new AuditTrailType(crt, upd, vod);

        return new MutasiBrgModel(
            MutasiBrgId,
            MutasiBrgDate,
            State,
            new LayananReff(LayananAsalId, LayananAsalName),
            new LayananReff(LayananTujuanId, LayananTujuanName),
            OrderMutasiId,
            DispatchNote,
            IssuerUserId,
            auditTrail,
            listItem?.ToList() ?? []);
    }
}
