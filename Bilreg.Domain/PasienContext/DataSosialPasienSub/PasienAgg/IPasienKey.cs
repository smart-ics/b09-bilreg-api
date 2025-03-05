using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public interface IPasienKey
{
    string PasienId { get; }
}

public class PasienKey : IPasienKey
{
    public PasienKey(string pasienId)
    {
        Guard.IsNotNullOrWhiteSpace(pasienId);
        PasienId = pasienId;
    }
    public string PasienId { get; }
}