using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameScopeItemDto(
    string SessionId,
    string BrgId,
    string BrgName = "")
{
    public StockOpnameScopeItemDto(string SessionId, string BrgId)
        : this(SessionId, BrgId, string.Empty)
    {
    }

    public StockOpnameScopeItemDto()
        : this(string.Empty, string.Empty, string.Empty)
    {
    }

    public static StockOpnameScopeItemDto FromModel(StockOpnameScopeItemModel model) =>
        new(
            SessionId: model.SessionId,
            BrgId: model.BrgId,
            BrgName: model.BrgName
        );

    public StockOpnameScopeItemModel ToModel() =>
        new(
            scopeItemId: string.Empty,
            sessionId: SessionId,
            brgId: BrgId,
            brgName: BrgName ?? string.Empty
        );
}
