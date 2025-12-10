using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public record TindakanDto(
    string TindakanId,
    DateTime TindakanDate,
    int JenisTindakan,
    string OrderId,
    string RegId,
    string PasienId,
    string PasienName,
    string LayananId,
    string LayananName,
    string TipeTarifId,
    string TipeTarifName,
    string TarifId,
    string TarifName,
    decimal Total,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate,
    DateTime OrderDate,
    string TarifOrderId,
    string TarifOrderName,
    IEnumerable<TindakanKomponenDto> Komponen)
{
    public static TindakanDto FromModel(TindakanModel model)
    {
        var komponenDtoList = model.Tarif.ListKomponen.Select(k => TindakanKomponenDto.FromModel(model.TindakanId, 
            model.Tarif.Tarif.TarifId, k)).ToList();

        var result = new TindakanDto(
            model.TindakanId, model.TindakanDate, (int)model.JenisTindakan,
            model.OrderTindakan.OrderId, 
            model.Reg.RegId, model.Pasien.PasienId, model.Pasien.PasienName,
            model.Layanan.LayananId, model.Layanan.LayananName,
            model.TipeTarif.TipeTarifId, model.TipeTarif.TipeTarifName,
            model.Tarif.Tarif.TarifId, model.Tarif.Tarif.TarifName,
            model.Tarif.Total,
            model.AuditTrail.Created.UserId, model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId, model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId, model.AuditTrail.Voided.Timestamp,
            model.OrderTindakan.OrderDate,
            model.OrderTindakan.Tindakan.TarifId,
            model.OrderTindakan.Tindakan.TarifName,
            Komponen: komponenDtoList
        );
        return result;
    }


    public TindakanModel ToModel(IEnumerable<TindakanKomponenDto> komponens)
    {
        var pasienReff = new PasienReff(PasienId, PasienName, new DateOnly(3000, 1, 1), "X"); 
        var regReff = new RegReff(RegId, PasienId, PasienName); 
        var layananReff = new LayananReff(LayananId, LayananName);
        var tarifReff = new TarifReff(TarifId, TarifName);

        var listKomponen = komponens.Select(k => k.ToModel()).ToList();
        var tarifData = new TarifType(TarifId, TarifName, GroupTarifType.Default,
        GroupTarifDkType.Default, JenisTarifType.Default);

        var tipeTarif = new TipeTarifReff(TipeTarifId, TipeTarifName);
        var tarifModel = new TindakanTarifModel(
            tarif: tarifData,
            listKomponen: listKomponen
        );

        var crt = new AuditInfoType(CrtUser, CrtDate);
        var upd = new AuditInfoType(UpdUser, UpdDate);
        var vod = new AuditInfoType(VodUser, VodDate);
        var auditTrail = new AuditTrailType(crt, upd, vod);
        
        var tarifOrder = new TarifReff(TarifOrderId, TarifOrderName);
        var orderTindakan = new OrderTindakanReff(OrderId, OrderDate, tarifOrder); 

        var result = new TindakanModel(
            TindakanId,
            TindakanDate,
            (JenisTindakanEnum)JenisTindakan,
            auditTrail,
            orderTindakan,
            pasienReff,
            regReff,
            layananReff,
            tipeTarif,
            tarifModel
        );
        return result;
    }

    public TindakanView ToView()
    {
        var regReff = new RegReff(RegId, PasienId, PasienName);
        var tipeTarif = new TipeTarifReff(TipeTarifId, TipeTarifName);
        var tarifReff = new TarifReff(TarifId, TarifName);
        var result = new TindakanView(TindakanId, TindakanDate, 
            (JenisTindakanEnum)JenisTindakan, regReff, tipeTarif, tarifReff);
        return result;
    }
}

public record TindakanKomponenDto(
    string TindakanId,
    string TarifId, 
    int NoUrut, 
    string KomponenTarifId,
    string KomponenTarifName,
    string PpaId,
    string PpaName,
    decimal Qty,
    decimal Nilai)
{
    public static TindakanKomponenDto FromModel(string tindakanId, string TarifId, TindakanKomponenTarifModel model)
    {
        var result = new TindakanKomponenDto(
            TindakanId: tindakanId,
            TarifId: TarifId,
            NoUrut: model.NoUrut, 
            KomponenTarifId: model.Komponen.KomponenId, 
            KomponenTarifName: model.Komponen.KomponenName,
            PpaId: model.Ppa.PpaId,
            PpaName: model.Ppa.PpaName,
            Qty: model.Qty,
            Nilai: model.Nilai
        );
        return result;
    }

    public TindakanKomponenTarifModel ToModel()
    {
        var komponenReff = new KomponenReff(KomponenTarifId, KomponenTarifName);
        var ppaReff = new PpaReff(PpaId, PpaName);

        var result = new TindakanKomponenTarifModel(
            Komponen: komponenReff,
            Ppa: ppaReff,
            NoUrut: NoUrut,
            Qty: Qty,
            Nilai: Nilai
        );
        return result;
    }
}
