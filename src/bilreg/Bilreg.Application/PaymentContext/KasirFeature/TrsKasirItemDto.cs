using Bilreg.Domain.PaymentContext.KasirFeature;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record TrsKasirItemDto(
    string KasirItemId,
    string TrsKasirId,
    int NoUrut,
    string BillId,
    string RegId,
    string LayananId,
    string Keterangan,
    decimal NilaiBill,
    decimal NilaiBayar,
    string crttgl = "",
    string crtjam = "",
    string crtusr = "")
{
    public static TrsKasirItemDto FromModel(TrsKasirItemType model, string userId = "")
    {
        return new TrsKasirItemDto(
            model.KasirItemId,
            model.TrsKasirId,
            model.NoUrut,
            model.BillId,
            model.RegId,
            model.LayananId,
            model.Keterangan,
            model.NilaiBill,
            model.NilaiBayar,
            DateTime.Now.ToString("yyyy-MM-dd"),
            DateTime.Now.ToString("HH:mm:ss"),
            userId);
    }

    public TrsKasirItemType ToModel()
    {
        return new TrsKasirItemType(
            KasirItemId,
            TrsKasirId,
            NoUrut,
            BillId,
            RegId,
            LayananId,
            Keterangan,
            NilaiBill,
            NilaiBayar);
    }
}
