using Bilreg.Application.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PurchaseContext.PurchaseOrderFeature;

public record PurchaseOrderDto(
    string PurchaseOrderId,
    string TglTrs,
    string JamTrs,
    bool IsClosed,
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
    string TglOpen,
    string JamOpen,
    string UserOpenId,
    string TglClose,
    string JamClose,
    string UserCloseId,
    string TglApprove,
    string JamApprove,
    string UserApproveId,
    string TglPrint,
    string JamPrint,
    string UserPrintId,
    string PartnerName
)
{
    public static PurchaseOrderDto FromModel(PurchaseOrderModel model)
    {
        var partner = model.Partner;
        var auditTrailOpen = model.Opened;
        var auditTrailClose = model.Closed;
        var auditTrailApprove = model.Approved;
        var auditTrailPrint = model.Printed;
        var auditTrailCreated = model.AuditTrail.Created;
        var auditTrailVoided = model.AuditTrail.Voided;
        
        var dto = new PurchaseOrderDto(
            model.PurchaseOrderId,
            auditTrailCreated.Timestamp.ToString(DateFormatEnum.YMD),
            auditTrailCreated.Timestamp.ToString(DateFormatEnum.HMS),
            model.IsClosed,
            model.Keterangan, 
            partner.PartnerId, 
            model.SubTotal,
            model.TaxTotal,
            model.Total,
            model.DiskonLain,
            model.BiayaLain,
            model.GrandTotal,
            auditTrailCreated.UserId,
            auditTrailVoided.Timestamp.ToString(DateFormatEnum.YMD),
            auditTrailVoided.Timestamp.ToString(DateFormatEnum.HMS),
            auditTrailVoided.UserId,
            auditTrailOpen.Timestamp.ToString(DateFormatEnum.YMD),
            auditTrailOpen.Timestamp.ToString(DateFormatEnum.HMS),
            auditTrailOpen.UserId,
            auditTrailClose.Timestamp.ToString(DateFormatEnum.YMD),
            auditTrailClose.Timestamp.ToString(DateFormatEnum.HMS),
            auditTrailClose.UserId,
            auditTrailApprove.Timestamp.ToString(DateFormatEnum.YMD),
            auditTrailApprove.Timestamp.ToString(DateFormatEnum.HMS),
            auditTrailApprove.UserId,
            auditTrailPrint.Timestamp.ToString(DateFormatEnum.YMD),
            auditTrailPrint.Timestamp.ToString(DateFormatEnum.HMS),
            auditTrailPrint.UserId,
            partner.PartnerName);
        return dto;
    }

    public PurchaseOrderModel ToModel(IEnumerable<PurchaseOrderItemType> listItem)
    {
        var created = new AuditInfoType(UserId, TglTrs, JamTrs);
        var voided = string.IsNullOrWhiteSpace(UserVoidId) || UserVoidId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildVoidAudit();
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, voided);
        
        var opened = string.IsNullOrWhiteSpace(UserOpenId) || UserOpenId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildOpenAudit();
        
        var closed = string.IsNullOrWhiteSpace(UserCloseId) || UserCloseId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildCloseAudit();
        
        var approved = string.IsNullOrWhiteSpace(UserApproveId) || UserApproveId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildApproveAudit();
        
        var printed = string.IsNullOrWhiteSpace(UserPrintId) || UserPrintId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildPrintAudit();

        var partner = new PartnerReff(PartnerId, PartnerName);
        var model = new PurchaseOrderModel(PurchaseOrderId, IsClosed, Keterangan, partner, SubTotal, TaxTotal, Total, DiskonLain, BiayaLain,
            GrandTotal, opened, closed, approved, printed, auditTrail, listItem);
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
    
    private AuditInfoType BuildOpenAudit()
    {
        if (string.IsNullOrWhiteSpace(TglOpen) || string.IsNullOrWhiteSpace(JamOpen))
            return AuditInfoType.Default;

        try
        {
            return new AuditInfoType(UserOpenId, TglOpen, JamOpen);
        }
        catch
        {
            return AuditInfoType.Default;
        }
    }
    
    private AuditInfoType BuildCloseAudit()
    {
        if (string.IsNullOrWhiteSpace(TglClose) || string.IsNullOrWhiteSpace(JamClose))
            return AuditInfoType.Default;

        try
        {
            return new AuditInfoType(UserCloseId, TglClose, JamClose);
        }
        catch
        {
            return AuditInfoType.Default;
        }
    }
    
    private AuditInfoType BuildApproveAudit()
    {
        if (string.IsNullOrWhiteSpace(TglApprove) || string.IsNullOrWhiteSpace(JamApprove))
            return AuditInfoType.Default;

        try
        {
            return new AuditInfoType(UserApproveId, TglApprove, JamApprove);
        }
        catch
        {
            return AuditInfoType.Default;
        }
    }
    
    private AuditInfoType BuildPrintAudit()
    {
        if (string.IsNullOrWhiteSpace(TglPrint) || string.IsNullOrWhiteSpace(JamPrint))
            return AuditInfoType.Default;

        try
        {
            return new AuditInfoType(UserPrintId, TglPrint, JamPrint);
        }
        catch
        {
            return AuditInfoType.Default;
        }
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