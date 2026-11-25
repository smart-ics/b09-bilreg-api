using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public class StockOpnameScopeItemModel
{
    #region PROPERTIES
    public string ScopeItemId { get; private set; }
    public string SessionId { get; private set; }
    public string BrgId { get; private set; }
    public string BrgName { get; private set; }
    #endregion

    #region CREATION
    public StockOpnameScopeItemModel(string scopeItemId, string sessionId, string brgId, string brgName = "")
    {
        ScopeItemId = scopeItemId?.Trim() ?? string.Empty;
        SessionId = sessionId?.Trim() ?? string.Empty;
        BrgId = brgId?.Trim() ?? string.Empty;
        BrgName = brgName?.Trim() ?? string.Empty;
    }

    public static StockOpnameScopeItemModel Create(string scopeItemId, string sessionId, string brgId, string brgName = "")
    {
        Guard.Against.NullOrWhiteSpace(sessionId, nameof(sessionId));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        return new StockOpnameScopeItemModel(scopeItemId, sessionId, brgId, brgName);
    }
    #endregion
}
