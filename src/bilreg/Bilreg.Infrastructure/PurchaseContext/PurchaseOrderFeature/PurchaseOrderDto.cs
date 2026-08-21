using Bilreg.Application.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PurchaseContext.PurchaseOrderFeature;

public record PurchaseOrderDto(
    string PurchaseOrderId,
    string TglTrs,
    string JamTrs,
    string Keterangan,
    string PartnerId,
    decimal SubTotal,
    decimal TaxTotal,
    decimal Total,
    decimal DiskonLain,
    decimal BiayaLain,
    decimal GrandTotal,
    string UserId,
    string TglVoid,
    string JamVoid,
    string UserVoidId,
    string PartnerName
)
{
    public static PurchaseOrderDto FromModel(PurchaseOrderModel model)
    {
        var partner = model.Partner;
        var auditTrailCreated = model.AuditTrail.Created;
        var auditTrailVoided = model.AuditTrail.Voided;
        var dto = new PurchaseOrderDto(model.PurchaseOrderId, auditTrailCreated.Timestamp.ToString(DateFormatEnum.YMD),
            auditTrailCreated.Timestamp.ToString(DateFormatEnum.HMS), model.Keterangan, partner.PartnerId, model.SubTotal,
            model.TaxTotal, model.Total, model.DiskonLain, model.BiayaLain, model.GrandTotal, auditTrailCreated.UserId,
            auditTrailVoided.Timestamp.ToString(DateFormatEnum.YMD), auditTrailVoided.Timestamp.ToString(DateFormatEnum.HMS),
            auditTrailVoided.UserId, partner.PartnerName);
        return dto;
    }

    public PurchaseOrderModel ToModel(IEnumerable<PurchaseOrderItemType> listItem)
    {
        var created = new AuditInfoType(UserId, TglTrs, JamTrs);
        var voided = string.IsNullOrWhiteSpace(UserVoidId) || UserVoidId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildVoidAudit();
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, voided);

        var partner = new PartnerReff(PartnerId, PartnerName);
        var model = new PurchaseOrderModel(PurchaseOrderId, Keterangan, partner, SubTotal, TaxTotal, Total, DiskonLain, BiayaLain,
            GrandTotal, auditTrail, listItem);
        return model;
    }
    
    public PurchaseOrderView ToView()
    {
        var created = new AuditInfoType(UserId, TglTrs, JamTrs);
        var voided = string.IsNullOrWhiteSpace(UserVoidId) || UserVoidId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildVoidAudit();
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, voided);

        var partner = new PartnerReff(PartnerId, PartnerName);
        var model = new PurchaseOrderView(PurchaseOrderId, Keterangan, partner, SubTotal, TaxTotal, Total, DiskonLain, BiayaLain,
            GrandTotal, auditTrail);
        return model;
    }

    private AuditInfoType BuildVoidAudit()
    {
        if (string.IsNullOrWhiteSpace(TglVoid) || string.IsNullOrWhiteSpace(JamVoid))
            return AuditInfoType.Default;

        try
        {
            return new AuditInfoType(UserVoidId, TglVoid, JamVoid);
        }
        catch
        {
            return AuditInfoType.Default;
        }
    }
}