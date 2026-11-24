namespace Bilreg.Application.PaymentContext.KasirFeature;

public record KasirItemBayarInput(
    string BillId,
    string RegId = "",
    string LayananId = "",
    string Keterangan = "",
    decimal NilaiBill = 0,
    decimal NilaiBayar = 0);

public record KasirItemBayarResponse(
    string KasirItemId,
    int NoUrut,
    string BillId,
    string RegId,
    string LayananId,
    string Keterangan,
    decimal NilaiBill,
    decimal NilaiBayar);
