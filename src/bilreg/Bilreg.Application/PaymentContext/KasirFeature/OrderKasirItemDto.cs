namespace Bilreg.Application.PaymentContext.KasirFeature;

public record OrderKasirItemDto(
    string OrderId,
    string Type,
    string Subtype,
    string RegId,
    string NoRm,
    string PasienName,
    decimal TotalNilai,
    string Status,
    string TglOrder,
    string UnitName = "-",
    string PenjaminName = "UMUM/MANDIRI");
