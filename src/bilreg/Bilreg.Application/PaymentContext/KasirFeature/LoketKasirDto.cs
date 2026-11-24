namespace Bilreg.Application.PaymentContext.KasirFeature;

public record LoketKasirDto(
    string LoketId,
    string NamaLoket,
    string CoaId,
    string NamaCoa,
    bool IsAktif,
    string Keterangan = "");
