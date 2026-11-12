namespace Bilreg.Domain.InventoryContext.PakaiBrgFeature;

public interface IPakaiBrgKey
{
    string PakaiBrgId { get; }
}

public record PakaiBrgKey(string PakaiBrgId) : IPakaiBrgKey;
