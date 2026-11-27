namespace Bilreg.Domain.InventoryContext.MutasiFeature;

public interface ITerimaMutasiKey
{
    string TerimaMutasiId { get; }
}

public record TerimaMutasiKey(string TerimaMutasiId) : ITerimaMutasiKey;
