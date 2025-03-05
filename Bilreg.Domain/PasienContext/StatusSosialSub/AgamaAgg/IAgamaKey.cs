namespace Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;

public interface IAgamaKey
{
    string AgamaId { get; }
}

public record AgamaKey(string AgamaId) : IAgamaKey;