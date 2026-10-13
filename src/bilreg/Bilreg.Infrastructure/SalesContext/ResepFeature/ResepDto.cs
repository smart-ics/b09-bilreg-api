using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.SalesContext.ResepFeature;

public class ResepDto : IResepKey
{
    public string ResepId { get; set; } = string.Empty;
    public DateTime TglJam { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string RegId { get; set; } = string.Empty;
    public string PasienId { get; set; } = string.Empty;
    public string PasienName { get; set; } = string.Empty;

    public decimal TinggiBadan { get; set; }
    public decimal BeratBadan { get; set; }
    public decimal Lpb { get; set; }

    public string LayananId { get; set; } = string.Empty;
    public string LayananName { get; set; } = string.Empty;
    public string LayananStokId { get; set; } = string.Empty;
    public string LayananStokName { get; set; } = string.Empty;
    public string DokterId { get; set; } = string.Empty;
    public string DokterName { get; set; } = string.Empty;

    public string UrgenitasId { get; set; } = string.Empty;
    public string UrgenitasName { get; set; } = string.Empty;
    public int Iter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TipeBarangId { get; set; } = string.Empty;
    public string TipeBarangName { get; set; } = string.Empty;

    public DateTime TglRencanaPeriksa { get; set; }
    public string ResepAsalReffId { get; set; } = string.Empty;
    public string VoidReason { get; set; } = string.Empty;

    public string TglVoid { get; set; } = string.Empty;
    public string JamVoid { get; set; } = string.Empty;
    public string UserVoidId { get; set; } = string.Empty;

    public static ResepDto FromModel(ResepModel model)
    {
        var desc = model.Description ?? string.Empty;
        if (model.IsVoid && !string.IsNullOrWhiteSpace(model.VoidReason) && model.VoidReason != AppConst.DASH)
        {
            if (!desc.StartsWith("[BATAL:", StringComparison.OrdinalIgnoreCase))
            {
                desc = string.IsNullOrWhiteSpace(desc) || desc == AppConst.DASH
                    ? $"[BATAL: {model.VoidReason}]"
                    : $"[BATAL: {model.VoidReason}] {desc}";
            }
        }

        return new ResepDto
        {
            ResepId = model.ResepId,
            TglJam = model.AuditTrail.Created.Timestamp,
            UserId = model.AuditTrail.Created.UserId,
            RegId = model.Register.RegId,
            PasienId = model.Register.PasienId,
            PasienName = model.Register.PasienName,
            TinggiBadan = model.BodyMetric.BodyHeight,
            BeratBadan = model.BodyMetric.BodyWeight,
            Lpb = model.BodyMetric.LingkarPinggang,
            LayananId = model.Layanan.LayananId,
            LayananName = model.Layanan.LayananName,
            LayananStokId = model.LayananStok.LayananId,
            LayananStokName = model.LayananStok.LayananName,
            DokterId = model.Dokter.DokterId,
            DokterName = model.Dokter.DokterName,
            UrgenitasId = model.Urgenitas.UrgenitasId,
            UrgenitasName = model.Urgenitas.UrgenitasName,
            Iter = model.Iter,
            Description = desc,
            TipeBarangId = model.TipeBrg.TipeBrgId,
            TipeBarangName = model.TipeBrg.TipeBrgName,
            TglRencanaPeriksa = model.TglRencanaPeriksa,
            ResepAsalReffId = model.ResepAsalReffId,
            VoidReason = model.VoidReason,
            TglVoid = model.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.YMD),
            JamVoid = model.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.HMS),
            UserVoidId = model.AuditTrail.Voided.UserId
        };
    }

    public ResepModel ToModel(IEnumerable<ResepBrgDto> listBrg)
    {
        var created = new AuditInfoType(UserId, TglJam);
        var voided = string.IsNullOrWhiteSpace(UserVoidId) || UserVoidId == AppConst.DASH
            ? AuditInfoType.Default
            : BuildVoidAudit();
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, voided);

        var reg = new RegReff(RegId, PasienId, PasienName);
        var bodyMetric = new BodyMetricType(BeratBadan, TinggiBadan, Lpb);
        var dokter = new DokterReff(DokterId, DokterName);
        var layanan = new LayananReff(LayananId, LayananName);
        var layananStok = new LayananReff(
            string.IsNullOrWhiteSpace(LayananStokId) ? LayananId : LayananStokId,
            string.IsNullOrWhiteSpace(LayananStokName) ? LayananName : LayananStokName);

        var urgenitas = UrgenitasType.Load(
            string.IsNullOrWhiteSpace(UrgenitasId) ? AppConst.DASH : UrgenitasId,
            UrgenitasName);
        var tipeBrg = new TipeBrgReff(
            string.IsNullOrWhiteSpace(TipeBarangId) ? AppConst.DASH : TipeBarangId,
            string.IsNullOrWhiteSpace(TipeBarangName) ? AppConst.DASH : TipeBarangName);

        var desc = Description ?? string.Empty;
        var voidReason = VoidReason ?? AppConst.DASH;
        if (desc.StartsWith("[BATAL:", StringComparison.OrdinalIgnoreCase))
        {
            var closeBracket = desc.IndexOf(']');
            if (closeBracket > 7)
            {
                var extracted = desc.Substring(7, closeBracket - 7).Trim();
                if (!string.IsNullOrWhiteSpace(extracted))
                    voidReason = extracted;
                desc = desc.Substring(closeBracket + 1).Trim();
            }
        }

        var tglRencana = TglRencanaPeriksa == default || TglRencanaPeriksa == new DateTime(3000, 1, 1)
            ? (TglJam != default ? TglJam.Date : DateTime.Today)
            : TglRencanaPeriksa;

        var resepAsalReffId = string.IsNullOrWhiteSpace(ResepAsalReffId) ? AppConst.DASH : ResepAsalReffId;

        var listObat = new List<ResepObatType>();
        foreach (var item in (listBrg ?? []).OrderBy(x => x.NoUrut))
        {
            if (item.IsKomponen)
            {
                var obatRacik = listObat.FirstOrDefault(x => x.Brg.BrgId == item.RacikId);
                if (obatRacik is null)
                    continue;

                var brgStub = BrgObatType.Default with
                {
                    BrgId = item.BrgId,
                    BrgName = item.BrgName.Trim()
                };
                var satuan = SatuanType.Create(item.SatuanId, item.SatuanName);
                obatRacik.AddItemRacik(brgStub, satuan, item.Qty, item.Dosis, item.DosisTxt);
            }
            else
            {
                listObat.Add(item.ToObatModel());
            }
        }

        return new ResepModel(
            ResepId, reg, bodyMetric, dokter, layanan, layananStok, urgenitas, tipeBrg,
            Iter, desc, tglRencana, resepAsalReffId, voidReason, auditTrail, listObat);
    }

    private AuditInfoType BuildVoidAudit()
    {
        if (string.IsNullOrWhiteSpace(TglVoid) || string.IsNullOrWhiteSpace(JamVoid) || TglVoid == "3000-01-01")
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
