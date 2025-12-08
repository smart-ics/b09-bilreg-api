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
    string OrderId,
    DateTime OrderDate,
    string TarifOrderId,
    string TarifOrderName,
    string RegId,
    string PasienId,
    string PasienName,
    string LayananId,
    string LayananName,
    string TarifId,
    string TarifName,
    decimal Total,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate,
    IEnumerable<TindakanKomponenDto> Komponen)
{
    public static TindakanDto FromModel(TindakanModel model)
    {
        var komponenDtoList = model.Tarif.ListKomponen.Select(k => TindakanKomponenDto.FromModel(model.TindakanId, 
            model.Tarif.Tarif.TarifId, k)).ToList();

        var result = new TindakanDto(
            TindakanId: model.TindakanId,
            TindakanDate: model.TindakanDate,
            OrderId: model.OrderTindakan.OrderId,
            OrderDate: model.OrderTindakan.OrderDate,
            TarifOrderId: model.OrderTindakan.Tindakan.TarifId,
            TarifOrderName: model.OrderTindakan.Tindakan.TarifName,
            RegId: model.Reg.RegId,
            PasienId: model.Pasien.PasienId,
            PasienName: model.Pasien.PasienName,
            LayananId: model.Layanan.LayananId,
            LayananName: model.Layanan.LayananName,
            TarifId: model.Tarif.Tarif.TarifId,
            TarifName: model.Tarif.Tarif.TarifName,
            Total: model.Tarif.Total,
            CrtUser: model.AuditTrail.Created.UserId,
            CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId,
            UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId,
            VodDate: model.AuditTrail.Voided.Timestamp,
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
            auditTrail,
            orderTindakan,
            pasienReff,
            regReff,
            layananReff,
            tarifModel
        );
        return result;
    }

    public TindakanView ToView()
    {
        var regReff = new RegReff(RegId, PasienId, PasienName);
        var tarifReff = new TarifReff(TarifId, TarifName);
        var result = new TindakanView(TindakanId, TindakanDate, regReff, tarifReff);
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
