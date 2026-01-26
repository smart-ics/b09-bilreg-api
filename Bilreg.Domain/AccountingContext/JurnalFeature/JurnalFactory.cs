using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.AccountingContext.JurnalFeature;

public interface IJurnalFactory
{
    JurnalType CreateFromReg(RegModel reg,
        KarcisType karcis, JaminanType jaminan, PpaType dokter,
        LayananType layanan, MapJaminanJkType mapJaminanJk,
        IEnumerable<KomponenType> listReffKomp);

    JurnalType CreateFromTindakan(TindakanModel tindakan,
        RegModel reg, TarifType tarif, JaminanType jaminan,
        LayananType layanan, MapJaminanJkType mapJaminanJk,
        IEnumerable<KomponenType> listReffKomp);
}

public class JurnalFactory : IJurnalFactory
{
    public JurnalType CreateFromReg(RegModel reg,
        KarcisType karcis, JaminanType jaminan, PpaType dokter,
        LayananType layanan, MapJaminanJkType mapJaminanJk,
        IEnumerable<KomponenType> listReffKomp)
    {
        var audit = AuditTrailType.Create(reg.RegMasukAudit.UserId, DateTime.Now);
        var jkReff = new JkReff(mapJaminanJk.JkId, string.Empty);
        var unitJk = new Jurnal2RLType(layanan.UnitPcc, jkReff);

        var pasienId = reg.Pasien.PasienId.Length >= 8
            ? reg.Pasien.PasienId[^8..]
            : reg.Pasien.PasienId.PadLeft(8, '0');
        var keterangan = new JurnalKetType($"{reg.RegId} - {pasienId} {reg.Pasien.PasienName}",
            $"{reg.RegId} - {pasienId}", string.Empty, string.Empty);

        var result = new JurnalType(reg.RegId, reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp)),
            audit.Created, keterangan, reg.ToReff(), reg.Pasien, []);

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        decimal nilaiPpdp = 0;
        var listReffKompFetched = listReffKomp.ToList();
        foreach (var item in reg.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);

            var rekPdpt = reffKomp?.RekPdpt?.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon?.CoaId ?? string.Empty;

            var ppa = reffKomp?.ListSatTugas?.Any() ?? false
                ? dokter.ToReff()
                : PpaType.Default.ToReff();

            if (item.Nilai > 0)
            {
                var jurnalNilaiPdp = new Jurnal2NilaiType(rekPdpt, $"Pendapatan {karcis.KarcisName}", 0, item.Nilai);
                var jurnalPdp = new Jurnal2JasaType(i++, jurnalNilaiPdp, unitJk, "", "", ppa.PpaId);
                result.AddJurnal2(jurnalPdp);
                nilaiPpdp += item.Nilai;
            }
            if (item.Diskon > 0)
            {
                var jurnalNilaiDiskon = new Jurnal2NilaiType(rekDiskon, $"Potongan pendapatan {karcis.KarcisName}", item.Diskon, 0);
                var jurnalDiskon = new Jurnal2JasaType(i++, jurnalNilaiDiskon, unitJk, "", "", ppa.PpaId);
                result.AddJurnal2(jurnalDiskon);
                nilaiPpdp -= item.Diskon;
            }
        }

        var jurnalNilaiPpdp = new Jurnal2NilaiType(rekPpdp, "Piutang Pasien Dalam Perawatan", nilaiPpdp, 0);
        var jurnalPpdp = new Jurnal2JasaType(i++, jurnalNilaiPpdp, unitJk, reg.RegDate.ToString("dd-MM-yyyy"), reg.RegId, "");
        result.AddJurnal2(jurnalPpdp);

        return result;
    }

    public JurnalType CreateFromTindakan(TindakanModel tindakan,
        RegModel reg, TarifType tarif, JaminanType jaminan,
        LayananType layanan, MapJaminanJkType mapJaminanJk,
        IEnumerable<KomponenType> listReffKomp)
    {
        var audit = AuditTrailType.Create(reg.RegMasukAudit.UserId, DateTime.Now);
        var jkReff = new JkReff(mapJaminanJk.JkId, string.Empty);
        var unitJk = new Jurnal2RLType(layanan.UnitPcc, jkReff);

        var pasienId = reg.Pasien.PasienId.Length >= 8
            ? reg.Pasien.PasienId[^8..]
            : reg.Pasien.PasienId.PadLeft(8, '0');
        var keterangan = new JurnalKetType($"{reg.RegId} - {pasienId} {reg.Pasien.PasienName}",
            $"{reg.RegId} - {pasienId}", string.Empty, string.Empty);

        var result = new JurnalType(tindakan.TindakanId, tindakan.TindakanDate,
            audit.Created, keterangan, reg.ToReff(), reg.Pasien, []);

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        decimal nilaiPpdp = 0;
        var listReffKompFetched = listReffKomp.ToList();
        foreach (var item in tindakan.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);

            var rekPdpt = reffKomp?.RekPdpt?.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon?.CoaId ?? string.Empty;

            var ppa = item is TindakanKomponenWithPpaType kompWithPpa
                ? kompWithPpa.Ppa
                : PpaType.Default.ToReff();

            if (item.Nilai > 0)
            {
                var jurnalNilaiPdp = new Jurnal2NilaiType(rekPdpt, $"Pendapatan {tarif.TarifName}", 0, item.Nilai);
                var jurnalPdp = new Jurnal2JasaType(i++, jurnalNilaiPdp, unitJk, "", "", ppa.PpaId);
                result.AddJurnal2(jurnalPdp);
                nilaiPpdp += item.Nilai;
            }
        }

        var jurnalNilaiPpdp = new Jurnal2NilaiType(rekPpdp, "Piutang Pasien Dalam Perawatan", nilaiPpdp, 0);
        var jurnalPpdp = new Jurnal2JasaType(i++, jurnalNilaiPpdp, unitJk, tindakan.TindakanDate.Date.ToString("dd-MM-yyyy"), reg.RegId, "");
        result.AddJurnal2(jurnalPpdp);

        return result;
    }
}
