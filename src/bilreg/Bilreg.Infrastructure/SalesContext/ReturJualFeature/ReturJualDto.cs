using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public class ReturJualDto
{
    public string ReturJualId { get; set; } = string.Empty;
    public DateTime TglJam { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string PenjualanId { get; set; } = string.Empty;
    public DateTime PenjualanDate { get; set; }
    public string RegId { get; set; } = string.Empty;
    public string PasienId { get; set; } = string.Empty;
    public string PasienName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public string LayananId { get; set; } = string.Empty;
    public string LayananName { get; set; } = string.Empty;

    public string TipeJaminanId { get; set; } = string.Empty;
    public string TipeJaminanName { get; set; } = string.Empty;
    public string TipeBrgId { get; set; } = string.Empty;
    public string TipeBrgName { get; set; } = string.Empty;

    public decimal SumSubTotalJual { get; set; }
    public decimal SumSubTotalRetur { get; set; }
    public decimal SumTax { get; set; }
    public decimal Pembulatan { get; set; }
    public decimal GrandTotal { get; set; }

    public string TglVoid { get; set; } = string.Empty;
    public string JamVoid { get; set; } = string.Empty;
    public string UserVoidId { get; set; } = string.Empty;

    public static ReturJualDto FromModel(ReturJualModel model)
    {
        return new ReturJualDto
        {
            ReturJualId = model.ReturJualId,
            TglJam = model.AuditTrail.Created.Timestamp,
            UserId = model.AuditTrail.Created.UserId,
            PenjualanId = model.Penjualan.PenjualanId,
            PenjualanDate = model.Penjualan.PenjualanDate,
            RegId = model.Penjualan.Reg.RegId,
            PasienId = model.Penjualan.Reg.PasienId,
            PasienName = model.Penjualan.Reg.PasienName,
            Reason = model.Reason,
            LayananId = model.Layanan.LayananId,
            LayananName = model.Layanan.LayananName,
            TipeJaminanId = model.TipeJaminan.TipeJaminanId,
            TipeJaminanName = model.TipeJaminan.TipeJaminanName,
            TipeBrgId = model.TipeBrg.TipeBrgId,
            TipeBrgName = model.TipeBrg.TipeBrgName,
            SumSubTotalJual = model.Nilai.SumSubTotalJual,
            SumSubTotalRetur = model.Nilai.SumSubTotalRetur,
            SumTax = model.Nilai.SumTax,
            Pembulatan = model.Nilai.Pembulatan,
            GrandTotal = model.Nilai.GrandTotal,
            TglVoid = model.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.YMD),
            JamVoid = model.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.HMS),
            UserVoidId = model.AuditTrail.Voided.UserId
        };
    }

    public ReturJualModel ToModel(IEnumerable<ReturJualItemDto> listItem)
    {
        var created = new AuditInfoType(UserId, TglJam);
        var voided = string.IsNullOrWhiteSpace(UserVoidId) || UserVoidId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildVoidAudit();
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, voided);

        var reg = new RegReff(RegId, PasienId, PasienName);
        var penjualan = new PenjualanReff(PenjualanId, PenjualanDate, reg);
        var layanan = new LayananReff(LayananId, LayananName);
        var tipeJaminan = new TipeJaminanReff(
            string.IsNullOrWhiteSpace(TipeJaminanId) ? AppConst.DASH : TipeJaminanId,
            string.IsNullOrWhiteSpace(TipeJaminanName) ? AppConst.DASH : TipeJaminanName);
        var tipeBrg = new TipeBrgReff(
            string.IsNullOrWhiteSpace(TipeBrgId) ? AppConst.DASH : TipeBrgId,
            string.IsNullOrWhiteSpace(TipeBrgName) ? AppConst.DASH : TipeBrgName);
        var nilai = new NilaiReturJualType(
            SumSubTotalJual, SumSubTotalRetur, SumTax, Pembulatan, GrandTotal);

        var listItems = new List<ReturJualItemModel>();
        foreach (var item in (listItem ?? []).OrderBy(x => x.NoUrut))
        {
            listItems.Add(item.ToItemModel());
        }

        return new ReturJualModel(
            ReturJualId,
            penjualan,
            layanan,
            Reason,
            tipeJaminan,
            tipeBrg,
            nilai,
            auditTrail,
            listItems
            );
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
