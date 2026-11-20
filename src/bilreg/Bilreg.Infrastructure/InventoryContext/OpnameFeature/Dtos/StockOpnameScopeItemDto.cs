using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameScopeItemDto(
    string ScopeItemId,
    string SessionId,
    string BrgId,
    string BrgName = "")
{
    public static StockOpnameScopeItemDto FromModel(StockOpnameScopeItemModel model) =>
        new(
            ScopeItemId: model.ScopeItemId,
            SessionId: model.SessionId,
            BrgId: model.BrgId,
            BrgName: model.BrgName
        );

    public StockOpnameScopeItemModel ToModel() =>
        new(
            scopeItemId: ScopeItemId,
            sessionId: SessionId,
            brgId: BrgId,
            brgName: BrgName ?? string.Empty
        );
}
