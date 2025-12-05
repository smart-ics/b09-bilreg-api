using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public record OrderTindakanDto(
    string OrderId,
    DateTime OrderDate,
    string PpaId,
    string PpaName,
    string RegId,
    string PasienId,
    string PasienName,
    string LayananId,
    string LayananName,
    string TarifId,
    string TindakanName)
{
    public static OrderTindakanDto FromModel(OrderTindakanModel model)
    {
        var result = new OrderTindakanDto(
            OrderId: model.OrderId,
            OrderDate: model.OrderDate,
            PpaId: model.DokterOrder.PpaId,
            PpaName: model.DokterOrder.PpaName, 
            RegId: model.Reg.RegId, 
            PasienId: model.Pasien.PasienId,
            PasienName: model.Pasien.PasienName,
            LayananId: model.Layanan.LayananId,
            LayananName: model.Layanan.LayananName,
            TarifId: model.Tindakan.TarifId,
            TindakanName: model.Tindakan.TarifName
        );
        return result;
    }

    public OrderTindakanModel ToModel()
    {

        var pasien = new PasienReff(PasienId, PasienName, new DateOnly(3000,1,1), "X");
        var reg = new RegReff(RegId, PasienId, PasienName);
        var ppa = new PpaReff(PpaId, PpaName);
        var layanan = new LayananReff(LayananId, LayananName);
        var tarif = new TarifReff(TarifId, TindakanName);
        
        var result = new OrderTindakanModel(OrderId, OrderDate, pasien, reg, ppa, layanan, tarif);
        return result;
    }
}
