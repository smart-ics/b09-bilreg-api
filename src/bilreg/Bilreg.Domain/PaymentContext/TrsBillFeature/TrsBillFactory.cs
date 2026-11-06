using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.DataTypeExtension;

namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

internal class TrsBillFactory
{
    public static TrsBillType CreateFromReg(RegModel reg,
        KarcisType karcis, JaminanType jaminan, PpaType dokter,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default)
    {
        var audit = AuditTrailType.Create(reg.RegMasukAudit.UserId, createdAt);
        var ketBilling = new TrsBillKetType($"REG : {karcis.KarcisName}", "", karcis.KarcisId, 1, reg.RegId);

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
                i++, komponen, TrsBillJenisBayarType.Pdp,
                item.Nilai, ppa, bill2Coa);

            listBill2TransEvent.Add(trsBill2);
        }

        var nilai = new TrsBillNilaiType(karcis.NilaiKarcis, reg.ListKomponen.Sum(x => x.Diskon), 0, 0);
        var result = new TrsBillType(reg.RegId, BillModulGroup.Jasa,
            reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp)),
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

        var expectedJaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3
            ? reg.TipeJaminan.TipeJaminanId[..3]
            : reg.TipeJaminan.TipeJaminanId;
        if (jaminan.JaminanId != expectedJaminanId)
            throw new ArgumentException("Jaminan tidak sesuai registrasi");

        var audit = AuditTrailType.Create(tindakan.AuditTrail.Created.UserId, createdAt);
        var ketBilling = new TrsBillKetType(tarif.TarifName, "", tarif.TarifId, tindakan.Qty, tindakan.TindakanId);

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
                i++, komponen, TrsBillJenisBayarType.Pdp,
                item.SubTotal, ppa, bill2Coa);

            listBill2TransEvent.Add(trsBill2);
        }

        var subTotal = tindakan.Total + tindakan.DiskonDokter;
        var nilai = new TrsBillNilaiType(subTotal, tindakan.DiskonDokter, 0, 0);

        var result = new TrsBillType(tindakan.TindakanId, BillModulGroup.Jasa, tindakan.TindakanDate,
            tindakan.Reg, tindakan.Layanan, tindakan.Kelas, audit.Created, tarif.RekapCetak,
            nilai, ketBilling, listBill2TransEvent, [], []);
        return result;
    }

    public static TrsBillType CreateFromLabOrderItem(LabOrderModel labOrder, LabOrderItemModel labOrderItem, RegModel reg,
        JaminanType jaminan, TarifType tarif, NilaiTarifType nilaiTarif, IEnumerable<KomponenType> listReffKomp, DateTime createdAt = default)
    {
        var expectedJaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3
            ? reg.TipeJaminan.TipeJaminanId[..3]
            : reg.TipeJaminan.TipeJaminanId;
        if (jaminan.JaminanId != expectedJaminanId)
            throw new ArgumentException("Jaminan tidak sesuai registrasi");

        var audit = AuditTrailType.Create(labOrder.AuditTrail.Created.UserId, createdAt);
        var ketBilling = new TrsBillKetType(tarif.TarifName, "", tarif.TarifId, 1, "");

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        var listReffKompFetched = listReffKomp.ToList();
        var listBill2TransEvent = new List<TrsBill2TransEventType>();

        nilaiTarif.ListKomponen.ForEach(item =>
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);

            var rekPdpt = reffKomp?.RekPdpt.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon.CoaId ?? string.Empty;

            var bill2Coa = new TrsBill2CoaType(
                new CoaType(rekPpdp, ""),
                new CoaType(rekPdpt, ""),
                CoaType.Default,
                CoaType.Default,
                CoaType.Default,
                new CoaType(rekDiskon, ""));

            var komponen = new TrsBill2KomponenType(item.Komponen.KomponenId, item.Komponen.KomponenName);

            var trsBill2 = new TrsBill2TransEventType(i++, komponen, TrsBillJenisBayarType.Pdp,
                item.Nilai, PpaType.Default.ToReff(), bill2Coa);

            listBill2TransEvent.Add(trsBill2);
        });

        var nilai = new TrsBillNilaiType(nilaiTarif.Nilai, 0, 0, 0);
        var billId = $"{labOrder.OrderId}-{labOrderItem.ItemNo:D2}";

        var result = new TrsBillType(billId, BillModulGroup.Jasa, DateTime.Now,
            reg.ToReff(), LayananType.Default.ToReff(), nilaiTarif.Kelas, audit.Created, tarif.RekapCetak,
            nilai, ketBilling, listBill2TransEvent, [], []);
        return result;
    }

    public static TrsBillType CreateFromPenjualanItem(
        PenjualanModel penjualan,
        PenjualanItemType item,
        RegModel reg,
        JaminanType jaminan,
        GroupRekType groupRek,
        DateTime createdAt = default)
    {
        var expectedJaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3
            ? reg.TipeJaminan.TipeJaminanId[..3]
            : reg.TipeJaminan.TipeJaminanId;
        if (jaminan.JaminanId != expectedJaminanId)
            throw new ArgumentException("Jaminan tidak sesuai registrasi");

        var audit = AuditTrailType.Create(penjualan.AuditTrail.Created.UserId, createdAt);
        var ketBilling = new TrsBillKetType(
            $"[{groupRek.GroupRekId}] {item.Brg.BrgName} {item.Qty:G29} {item.Satuan.SatuanName}",
            "",
            item.Brg.BrgId,
            item.Qty,
            penjualan.PenjualanId);

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpObatRanap
            : jaminan.Rekening.PpdpObatRajal;

        var rekPdpt = (reg.JenisReg != JenisRegEnum.RegInap && !string.IsNullOrWhiteSpace(groupRek.RekJualRajal.CoaId) && groupRek.RekJualRajal.CoaId != "-")
            ? groupRek.RekJualRajal
            : groupRek.RekJual;

        var ppa = new PpaReff(penjualan.Dokter.DokterId, penjualan.Dokter.DokterName);
        var komponen = new TrsBill2KomponenType(groupRek.GroupRekId, groupRek.GroupRekName);

        var bill2CoaPdp = new TrsBill2CoaType(
            rekPpdp,
            rekPdpt,
            groupRek.RekPersediaan,
            CoaType.Default,
            CoaType.Default,
            groupRek.RekDisc);

        var noUrut = 1;
        var listBill2TransEvent = new List<TrsBill2TransEventType>
        {
            new(
                noUrut++,
                komponen,
                TrsBillJenisBayarType.Pdp,
                item.Nilai.SubTotal,
                ppa,
                bill2CoaPdp)
        };

        if (item.Nilai.Tax > 0)
        {
            var bill2CoaTax = new TrsBill2CoaType(
                CoaType.Default,
                rekPdpt,
                CoaType.Default,
                CoaType.Default,
                groupRek.RekTax,
                CoaType.Default);

            listBill2TransEvent.Add(new(
                noUrut++,
                komponen,
                TrsBillJenisBayarType.Tax,
                item.Nilai.Tax,
                ppa,
                bill2CoaTax));
        }

        var rekapCetak = new RekapCetakReff(groupRek.RekapCetakId, "");
        var subTotalKotor = item.Nilai.SubTotal + item.Nilai.Diskon;
        var nilai = new TrsBillNilaiType(subTotalKotor, item.Nilai.Diskon, item.Nilai.Tax, item.Nilai.Embalase);

        var billDate = createdAt == default ? DateTime.Now : createdAt;
        var result = new TrsBillType(
            item.PenjualanItemId,
            BillModulGroup.Obat,
            billDate,
            reg.ToReff(),
            penjualan.Layanan,
            reg.Kelas,
            audit.Created,
            rekapCetak,
            nilai,
            ketBilling,
            listBill2TransEvent,
            [],
            []);

        return result;
    }
}
