using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OpCaseAktifDto(
    string OrderOpId, DateTime OrderOpDate, string PasienId, int OrderOpState,
    string PasienName, string TglLahir, string Gender,
    string NamaOperasi, int UrgencyLevel, DateTime PreferedDate)
{
    public static OpCaseAktifDto FromModel(OpCaseReff model)
    {
        var tglLahir = model.Pasien.TglLahir.ToString("yyyy-MM-dd");
        var result = new OpCaseAktifDto(model.OrderOpId,
            model.OrderOpDate, model.Pasien.PasienId,
            (int)model.OrderOpState,
            model.Pasien.PasienName, tglLahir, model.Pasien.Gender,
            "-", 0, new DateTime(3000,1,1));
        return result;
    }

    public OpCaseReff ToModel()
    {
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.Parse(TglLahir), Gender);
        var result = new OpCaseReff(OrderOpId, OrderOpDate, pasien,
            (OpCaseStateEnum)OrderOpState);
        return result;
    }

    public OpCaseOrderView ToView()
    {
        var result = new OpCaseOrderView(OrderOpId, 
            new PasienReff(PasienId, PasienName, DateOnly.Parse(TglLahir), Gender), 
            NamaOperasi, (UrgencyLevelEnum)UrgencyLevel, PreferedDate);
        return result;
    }
}
