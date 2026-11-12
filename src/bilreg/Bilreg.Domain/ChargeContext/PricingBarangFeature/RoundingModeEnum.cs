namespace Bilreg.Domain.ChargeContext.PricingBarangFeature;

/// <summary>
/// Arah pembulatan total tagihan kasir.
/// </summary>
public enum RoundingModeEnum
{
    /// <summary>
    /// Pembulatan ke kelipatan terdekat (Nearest / Round Half Up).
    /// Pecahan &gt;= 50% dari nilai acuan dibulatkan ke atas, di bawah 50% dibulatkan ke bawah.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Pembulatan ke atas (Ceiling / Round Up).
    /// Setiap pecahan di atas kelipatan nilai acuan dibulatkan ke kelipatan berikutnya.
    /// </summary>
    KeAtas = 1
}
