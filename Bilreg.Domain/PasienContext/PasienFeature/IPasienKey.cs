using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public interface IPasienKey
{
    string PasienId { get; }
}

public class PasienKey : IPasienKey
{
    public PasienKey(string pasienId)
    {
        Guard.Against.NullOrWhiteSpace(pasienId);
        PasienId = pasienId;
    }
    public string PasienId { get; }
}