using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Domain.ApotekContext.PenjualanFeature;

public record PenjualanKey : IPenjualanKey
{
    public const string IdPrefix = "DU";
    public const int IdLength = 10;

    public string PenjualanId { get; }

    public PenjualanKey(string penjualanId)
    {
        Guard.Against.NullOrWhiteSpace(penjualanId, nameof(penjualanId));
        if (penjualanId.Length != IdLength || !penjualanId.StartsWith(IdPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ApotekDomainException($"PenjualanId '{penjualanId}' tidak valid. Harus memiliki panjang {IdLength} karakter dengan prefix '{IdPrefix}'.");

        PenjualanId = penjualanId;
    }
}
