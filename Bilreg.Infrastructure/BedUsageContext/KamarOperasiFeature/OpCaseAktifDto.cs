using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OpCaseAktifDto(
    string OrderOpId, DateTime OrderOpDate, string PasienId, int OrderOpState,
    string PasienName, string TglLahir, string Gender)
{
    public static OpCaseAktifDto FromModel(OpCaseReff model)
    {
        var tglLahir = model.Pasien.TglLahir.ToString("yyyy-MM-dd");
        var result = new OpCaseAktifDto(model.OrderOpId,
            model.OrderOpDate, model.Pasien.PasienId,
            (int)model.OrderOpState,
            model.Pasien.PasienName, tglLahir, model.Pasien.Gender);
        return result;
    }

    public OpCaseReff ToModel()
    {
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.Parse(TglLahir), Gender);
        var result = new OpCaseReff(OrderOpId, OrderOpDate, pasien,
            (OrderOpStateEnum)OrderOpState);
        return result;
    }
}
