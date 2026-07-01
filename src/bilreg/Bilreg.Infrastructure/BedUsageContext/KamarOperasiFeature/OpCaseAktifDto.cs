using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OpCaseAktifDto(
    string OrderOpId, DateTime OrderOpDate, string PasienId, int OpCaseState,
    string PasienName, string TglLahir, string Gender, string NamaOperasi, 
    int EstimasiDurasi, int UrgencyLevel, DateTime PreferedDate, 
    string DokterId, string DokterName)
{
    public static OpCaseAktifDto FromModel(OpCaseReff model)
    {
        var tglLahir = model.Pasien.TglLahir.ToString("yyyy-MM-dd");
        var result = new OpCaseAktifDto(model.OrderOpId,
            model.OrderOpDate, model.Pasien.PasienId,
            (int)model.OpCaseState,
            model.Pasien.PasienName, tglLahir, model.Pasien.Gender,
            "-", 0, 0, new DateTime(3000,1,1), "-", "-");
        return result;
    }

    public OpCaseReff ToModel()
    {
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.Parse(TglLahir), Gender);
        var result = new OpCaseReff(OrderOpId, OrderOpDate, pasien,
            (OpCaseStateEnum)OpCaseState);
        return result;
    }

    public OpCaseOrderView ToView()
    {
        var result = new OpCaseOrderView(OrderOpId, 
            new PasienReff(PasienId, PasienName, DateOnly.Parse(TglLahir), Gender),
            NamaOperasi, new PpaReff(DokterId, DokterName), 
            EstimasiDurasi,(UrgencyLevelEnum)UrgencyLevel, PreferedDate, (OpCaseStateEnum)OpCaseState);
        return result;
    }
}
