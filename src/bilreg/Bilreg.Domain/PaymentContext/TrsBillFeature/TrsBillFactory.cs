using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

internal class TrsBillFactory
{
    public static TrsBillType CreateFromReg(RegModel reg,
        KarcisType karcis, JaminanType jaminan, PpaType dokter,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default)
    {
        var audit = AuditTrailType.Create(reg.RegMasukAudit.UserId, createdAt);
        var ketBilling = new TrsBillKetType($"REG : {karcis.KarcisName}", "", karcis.KarcisId, 1, "");

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        var listReffKompFetched = listReffKomp.ToList();
        var listBill2TransEvent = new List<TrsBill2TransEventType>();

        foreach (var item in reg.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);
            
            var rekPdpt = reffKomp?.RekPdpt.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon.CoaId ?? string.Empty;

            var ppa = reffKomp?.ListSatTugas.Any() ?? false
                ? dokter.ToReff()
                : PpaType.Default.ToReff();
            
            var bill2Coa = new TrsBill2CoaType(
                new CoaType(rekPpdp, ""),
                new CoaType(rekPdpt, ""),
                CoaType.Default,
                CoaType.Default,
                CoaType.Default,
                new CoaType(rekDiskon, ""));
            
            var komponen = new TrsBill2KomponenType(item.Komponen.KomponenId, item.Komponen.KomponenName);
            
            var trsBill2 = new TrsBill2TransEventType(
                i++,  komponen, TrsBillJenisBayarType.Pdp, 
                item.Nilai, ppa, bill2Coa);

            listBill2TransEvent.Add(trsBill2);
        }

        var nilai = new TrsBillNilaiType(karcis.NilaiKarcis, reg.ListKomponen.Sum(x => x.Diskon), 0, 0);
        var result = new TrsBillType(reg.RegId, BillModulGroup.Jasa, reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp)),
            reg.ToReff(), reg.Layanan, reg.Kelas, audit.Created, karcis.RekapCetak,  
            nilai, ketBilling, listBill2TransEvent, [], []);

        return result;
    }

    public static TrsBillType CreateFromTindakan(
        TindakanModel tindakan,
        RegModel reg, TarifType tarif, JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default)
    {
        if (tarif.ToReff() != tindakan.Tarif)
            throw new ArgumentException("Tarif tidak sesuai");

        if (jaminan.JaminanId != reg.TipeJaminan.TipeJaminanId[..3])
            throw new ArgumentException("Jaminan tidak sesuai registrasi");

        var audit = AuditTrailType.Create(tindakan.AuditTrail.Created.UserId, createdAt);
        var ketBilling = new TrsBillKetType(tarif.TarifName, "", tarif.TarifId, 1, "");

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;
        
        var i = 0;
        var listReffKompFetched = listReffKomp.ToList();
        var listBill2TransEvent = new List<TrsBill2TransEventType>();
        
        foreach (var item in tindakan.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);

            var rekPdpt = reffKomp?.RekPdpt.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon.CoaId ?? string.Empty;

            var ppa = item is TindakanKomponenWithPpaType kompWithPpa
                ? kompWithPpa.Ppa
                : PpaType.Default.ToReff();
            var bill2Coa = new TrsBill2CoaType(
                new CoaType(rekPpdp, ""),
                new CoaType(rekPdpt, ""),
                CoaType.Default,
                CoaType.Default,
                CoaType.Default,
                new CoaType(rekDiskon, ""));
            
            var komponen = new TrsBill2KomponenType(item.Komponen.KomponenId, item.Komponen.KomponenName);
            
            var trsBill2 = new TrsBill2TransEventType(
                i++,  komponen, TrsBillJenisBayarType.Pdp, 
                item.Nilai, ppa, bill2Coa);

            listBill2TransEvent.Add(trsBill2);
        }

        var nilai = new TrsBillNilaiType(tindakan.Total, 0, 0, 0);
        
        var result = new TrsBillType(tindakan.TindakanId, BillModulGroup.Jasa, tindakan.TindakanDate,
            tindakan.Reg, tindakan.Layanan, tindakan.Kelas, audit.Created, tarif.RekapCetak, 
            nilai, ketBilling, listBill2TransEvent, [], []);
        return result;
    }
}
