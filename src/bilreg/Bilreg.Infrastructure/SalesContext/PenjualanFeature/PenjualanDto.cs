using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.SalesContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.SalesContext.PenjualanFeature;

public class PenjualanDto
{
    public string PenjualanId { get; set; } = string.Empty;
    public DateTime TglJam { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string ResepId { get; set; } = string.Empty;
    public string RegId { get; set; } = string.Empty;
    public string PasienId { get; set; } = string.Empty;
    public string PasienName { get; set; } = string.Empty;

    public string LayananId { get; set; } = string.Empty;
    public string LayananName { get; set; } = string.Empty;
    public string LayananResepId { get; set; } = string.Empty;
    public string LayananResepName { get; set; } = string.Empty;
    public string DokterId { get; set; } = string.Empty;
    public string DokterName { get; set; } = string.Empty;

    public string TipeJaminanId { get; set; } = string.Empty;
    public string TipeJaminanName { get; set; } = string.Empty;
    public string TipeBarangId { get; set; } = string.Empty;
    public string TipeBarangName { get; set; } = string.Empty;

    public decimal SumSubTotal { get; set; }
    public decimal SumBiaya { get; set; }
    public decimal SumTax { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiskonLain { get; set; }
    public decimal BiayaLain { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal Pembulatan { get; set; }
    public decimal Bulat { get; set; }

    public string TglVoid { get; set; } = string.Empty;
    public string JamVoid { get; set; } = string.Empty;
    public string UserVoidId { get; set; } = string.Empty;

    public static PenjualanDto FromModel(PenjualanModel model)
    {
        return new PenjualanDto
        {
            PenjualanId = model.PenjualanId,
            TglJam = model.AuditTrail.Created.Timestamp,
            UserId = model.AuditTrail.Created.UserId,
            ResepId = model.ResepId,
            RegId = model.Register.RegId,
            PasienId = model.Register.PasienId,
            PasienName = model.Register.PasienName,
            LayananId = model.Layanan.LayananId,
            LayananName = model.Layanan.LayananName,
            LayananResepId = model.LayananResep.LayananId,
            LayananResepName = model.LayananResep.LayananName,
            DokterId = model.Dokter.DokterId,
            DokterName = model.Dokter.DokterName,
            TipeJaminanId = model.TipeJaminan.TipeJaminanId,
            TipeJaminanName = model.TipeJaminan.TipeJaminanName,
            TipeBarangId = model.TipeBrg.TipeBrgId,
            TipeBarangName = model.TipeBrg.TipeBrgName,
            SumSubTotal = model.Nilai.SumSubTotal,
            SumBiaya = model.Nilai.SumBiaya,
            SumTax = model.Nilai.SumTax,
            SubTotal = model.Nilai.SubTotal,
            DiskonLain = model.Nilai.DiskonLain,
            BiayaLain = model.Nilai.BiayaLain,
            GrandTotal = model.Nilai.GrandTotal,
            Pembulatan = model.Nilai.Pembulatan,
            Bulat = model.Nilai.Bulat,
            TglVoid = model.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.YMD),
            JamVoid = model.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.HMS),
            UserVoidId = model.AuditTrail.Voided.UserId
        };
    }

    public PenjualanModel ToModel(IEnumerable<PenjualanItemDto> listItem)
    {
        var created = new AuditInfoType(UserId, TglJam);
        var voided = string.IsNullOrWhiteSpace(UserVoidId) || UserVoidId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildVoidAudit();
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, voided);

        var reg = new RegReff(RegId, PasienId, PasienName);
        var dokter = new DokterReff(DokterId, DokterName);
        var layanan = new LayananReff(LayananId, LayananName);
        var layananResep = new LayananReff(LayananResepId, LayananResepName);
        var tipeJaminan = new TipeJaminanReff(
            string.IsNullOrWhiteSpace(TipeJaminanId) ? AppConst.DASH : TipeJaminanId,
            string.IsNullOrWhiteSpace(TipeJaminanName) ? AppConst.DASH : TipeJaminanName);
        var tipeBrg = new TipeBrgReff(
            string.IsNullOrWhiteSpace(TipeBarangId) ? AppConst.DASH : TipeBarangId,
            string.IsNullOrWhiteSpace(TipeBarangName) ? AppConst.DASH : TipeBarangName);
        var nilai = new NilaiPenjualanType(
            SumSubTotal, SumBiaya, SumTax, SubTotal, DiskonLain, BiayaLain, Pembulatan, Bulat, GrandTotal);

        var listItems = new List<PenjualanItemType>();
        foreach (var item in (listItem ?? []).OrderBy(x => x.NoUrut))
        {
            if (item.IsKomponen)
            {
                var parent = listItems.FirstOrDefault(x => x.Brg.BrgId == item.RacikId);
                if (parent is null)
                    continue;

                var brgStub = BrgObatType.Default with
                {
                    BrgId = item.BrgId,
                    BrgName = item.BrgName.Trim()
                };
                var satuan = SatuanType.Create(
                    string.IsNullOrWhiteSpace(item.SatuanId) ? AppConst.DASH : item.SatuanId,
                    string.IsNullOrWhiteSpace(item.SatuanName) ? AppConst.DASH : item.SatuanName);
                parent.AddItemRacik(brgStub, satuan, item.Qty, item.Dosis, item.DosisTxt);
            }
            else
            {
                listItems.Add(item.ToItemModel());
            }
        }

        return new PenjualanModel(
            PenjualanId,
            string.IsNullOrWhiteSpace(ResepId) ? AppConst.DASH : ResepId,
            reg,
            dokter,
            layanan,
            layananResep,
            tipeJaminan,
            tipeBrg,
            nilai,
            auditTrail,
            listItems);
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
