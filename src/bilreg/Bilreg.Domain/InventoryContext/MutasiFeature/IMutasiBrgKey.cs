namespace Bilreg.Domain.InventoryContext.MutasiFeature;

public interface IMutasiBrgKey
{
    string MutasiBrgId { get; }
}

public record MutasiBrgKey(string MutasiBrgId) : IMutasiBrgKey;
