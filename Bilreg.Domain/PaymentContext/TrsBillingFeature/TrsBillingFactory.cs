using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public interface ITrsBillingFactory
{
    TrsBillingType CreateFromTindakan(TindakanModel tindakan,
        RegModel reg, TarifType tarif, JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp);
    TrsBillingType CreateFromRegistrasi(RegModel reg,
        KarcisType karcis, JaminanType jaminan, PpaType dokter,
        IEnumerable<KomponenType> listReffKomp);
}
public class TrsBillingFactory : ITrsBillingFactory
{
    public TrsBillingType CreateFromRegistrasi(RegModel reg,
        KarcisType karcis, JaminanType jaminan, PpaType dokter,
        IEnumerable<KomponenType> listReffKomp)
    {
        var audit = AuditTrailType.Create(reg.RegMasukAudit.UserId, DateTime.Now);

        // Ambil PpaName dari komponen pertama yang memiliki ListSatTugas
        string ketMedis = "";
        var listReffKompFetched = listReffKomp.ToList();

        foreach (var item in reg.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);
            if (reffKomp?.ListSatTugas?.Any() == true)
            {
                ketMedis = dokter.PpaName;
                break;
            }
        }

        // Ambil maksimal 40 karakter
        if (ketMedis.Length > 40)
            ketMedis = ketMedis[..40];

        var ketBilling = new TrsBillKetType($"REG : {karcis.KarcisName}", ketMedis, karcis.KarcisId, 1, "");
        var result = new TrsBillingType(reg.RegId, 0, reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp)),
            reg.ToReff(), reg.Layanan, reg.Kelas, audit.Created, karcis.NilaiKarcis, reg.ListKomponen.Sum(x => x.Diskon), 0, 0,
            karcis.RekapCetak, ketBilling, []);

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        foreach (var item in reg.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);
            var rekPdpt = reffKomp?.RekPdpt?.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon?.CoaId ?? string.Empty;
            var rekJasa = new RekJasaType(rekPpdp, rekPdpt, rekDiskon);
            var ppa = reffKomp?.ListSatTugas?.Any() ?? false
                ? dokter.ToReff()
                : PpaType.Default.ToReff();

            var trsBill2 = new TrsBilling2JasaType(i++, reg.RegId, reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp)),
                new NilaiBillingType("PDP", item.Nilai, 0), ppa, PegType.Default,
                item.Komponen, rekJasa);
            result.AddTrsBilling2(trsBill2);
        }
        return result;
    }

    public TrsBillingType CreateFromTindakan(TindakanModel tindakan,
        RegModel reg, TarifType tarif, JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp)
    {
        if (tarif.ToReff() != tindakan.Tarif)
            throw new ArgumentException("Tarif tidak sesuai");
        if (jaminan.JaminanId != reg.TipeJaminan.TipeJaminanId[..3])
            throw new ArgumentException("Jaminan tidak sesuai registrasi");

        var audit = AuditTrailType.Create(tindakan.AuditTrail.Created.UserId, DateTime.Now);

        // Ambil nama-nama PPA dan gabungkan dengan separator ";"
        var ppaNames = tindakan.ListKomponen
            .OfType<TindakanKomponenWithPpaType>()
            .Select(komp => komp.Ppa?.PpaName ?? "")
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        // Gabungkan dengan separator dan ambil 40 karakter pertama
        var combinedPpaNames = string.Join(";", ppaNames);
        var ketMedis = combinedPpaNames[..Math.Min(40, combinedPpaNames.Length)];

        var ketBilling = new TrsBillKetType(tarif.TarifName, ketMedis, tarif.TarifId, 1, "");
        var result = new TrsBillingType(tindakan.TindakanId, 0, tindakan.TindakanDate,
            tindakan.Reg, tindakan.Layanan, tindakan.Kelas, audit.Created, tindakan.Total, 0, 0, 0,
            tarif.RekapCetak, ketBilling, []);

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        var listReffKompFetched = listReffKomp.ToList();
        foreach (var item in tindakan.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);

            var rekPdpt = reffKomp?.RekPdpt?.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon?.CoaId ?? string.Empty;
            var rekJasa = new RekJasaType(rekPpdp, rekPdpt, rekDiskon);

            var ppa = item is TindakanKomponenWithPpaType kompWithPpa
                ? kompWithPpa.Ppa
                : PpaType.Default.ToReff();

            var trsBill2 = new TrsBilling2JasaType(i++, tindakan.TindakanId, tindakan.TindakanDate,
                new NilaiBillingType("PDP", item.Nilai, 0), ppa, PegType.Default,
                item.Komponen, rekJasa);

            result.AddTrsBilling2(trsBill2);
        }
        return result;
    }
}
