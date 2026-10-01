using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

//  M03-F01 P2-S04 — Dapper DTO for trs_tindakan_lanjut (+item).
//  Column-aligned with the P1-S02 script: header identity, visit
//  reference, order type, receiving-service reference, state 0-3,
//  timestamps, RowVersion concurrency token, Crt/Upd/Vod audit.
public record TindakanLanjutDto(
    string TindakanLanjutId, DateTime TindakanLanjutDate,
    string RegId, string PasienId, string PasienName,
    string OrderType, string LayananId, string LayananName,
    int OrderState, DateTime SentDate, DateTime ReceivedDate,
    string ReceivedBy, string CancelReason, int RowVersion,
    string CrtUser, DateTime CrtDate,
    string UpdUser, DateTime UpdDate,
    string VodUser, DateTime VodDate)
{
    public static TindakanLanjutDto FromModel(TindakanLanjutModel model)
    {
        return new TindakanLanjutDto(
            model.TindakanLanjutId, model.TindakanLanjutDate,
            model.Reg.RegId, model.Reg.PasienId, model.Reg.PasienName,
            model.OrderType, model.Layanan.LayananId, model.Layanan.LayananName,
            (int)model.OrderState, model.SentDate, model.ReceivedDate,
            model.ReceivedBy, model.CancelReason, model.RowVersion,
            model.AuditTrail.Created.UserId, model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId, model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId, model.AuditTrail.Voided.Timestamp);
    }

    public TindakanLanjutModel ToModel(IEnumerable<TindakanLanjutItemModel> listItem)
    {
        var regReff = new RegReff(RegId, PasienId, PasienName);
        var layananReff = new LayananReff(LayananId, LayananName);
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));

        return new TindakanLanjutModel(
            TindakanLanjutId, TindakanLanjutDate,
            regReff, OrderType, layananReff,
            (TindakanLanjutStateEnum)OrderState,
            SentDate, ReceivedDate, ReceivedBy,
            CancelReason, RowVersion,
            listItem ?? [],
            auditTrail);
    }
}

public record TindakanLanjutItemDto(
    string TindakanLanjutId, int ItemNo,
    string ItemCode, string ItemName, decimal Qty, string Note)
{
    public static TindakanLanjutItemDto FromModel(TindakanLanjutItemModel model, string tindakanLanjutId)
    {
        return new TindakanLanjutItemDto(
            tindakanLanjutId, model.ItemNo,
            model.ItemCode, model.ItemName, model.Qty, model.Note);
    }

    public TindakanLanjutItemModel ToModel()
    {
        return new TindakanLanjutItemModel(ItemNo, ItemCode, ItemName, Qty, Note);
    }
}
