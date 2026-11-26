using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public record TerimaMutasiDto(
    string TerimaMutasiId,
    DateTime TerimaMutasiDate,
    TerimaMutasiStateEnum State,
    string MutasiBrgId,
    string OrderMutasiId,
    string LayananAsalId,
    string LayananAsalName,
    string LayananTujuanId,
    string LayananTujuanName,
    string ReceiverUserId,
    string Note,
    string RejectionReason,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static TerimaMutasiDto FromModel(TerimaMutasiModel model)
    {
        return new TerimaMutasiDto(
            model.TerimaMutasiId,
            model.TerimaMutasiDate,
            model.State,
            model.MutasiBrgId,
            model.OrderMutasiId,
            model.LayananAsal.LayananId,
            model.LayananAsal.LayananName,
            model.LayananTujuan.LayananId,
            model.LayananTujuan.LayananName,
            model.ReceiverUserId,
            model.Note,
            model.RejectionReason,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp
        );
    }

    public TerimaMutasiModel ToModel(IEnumerable<TerimaMutasiItemModel>? listItem = null)
    {
        var crt = new AuditInfoType(CrtUser, CrtDate);
        var upd = new AuditInfoType(UpdUser, UpdDate);
        var vod = new AuditInfoType(VodUser, VodDate);
        var auditTrail = new AuditTrailType(crt, upd, vod);

        return TerimaMutasiModel.Load(
            TerimaMutasiId,
            TerimaMutasiDate,
            State,
            MutasiBrgId,
            OrderMutasiId,
            new LayananReff(LayananAsalId, LayananAsalName),
            new LayananReff(LayananTujuanId, LayananTujuanName),
            ReceiverUserId,
            Note,
            RejectionReason,
            auditTrail,
            listItem?.ToList() ?? []);
    }

    public TerimaMutasiHeaderView ToHeaderView()
    {
        return new TerimaMutasiHeaderView(
            TerimaMutasiId,
            TerimaMutasiDate,
            State,
            MutasiBrgId,
            OrderMutasiId,
            new LayananReff(LayananAsalId, LayananAsalName),
            new LayananReff(LayananTujuanId, LayananTujuanName),
            ReceiverUserId,
            Note,
            RejectionReason);
    }
}
