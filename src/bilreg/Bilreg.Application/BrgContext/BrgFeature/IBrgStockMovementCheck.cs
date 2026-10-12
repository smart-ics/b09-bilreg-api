namespace Bilreg.Application.BrgContext.BrgFeature;

public interface IBrgStockMovementCheck
{
    bool HasStockMovementOrBalance(string brgId);
}
