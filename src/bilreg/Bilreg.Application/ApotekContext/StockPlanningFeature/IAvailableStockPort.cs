using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Application.ApotekContext.StockPlanningFeature;

public record AvailableStockRequest(string BrgId, string LayananId, decimal RequestedQty);

public record AvailableStockLine(string BrgId, decimal AvailableQty);

public record AvailableStockResult(bool Evaluated, string ErrorCode, IReadOnlyList<AvailableStockLine> Lines)
{
    public static AvailableStockResult FailClosed()
        => new(false, "PD09_AVAILABLE_STOCK_NOT_CONFIGURED", []);

    public decimal QtyFor(string brgId)
        => Lines.FirstOrDefault(x => x.BrgId == brgId)?.AvailableQty ?? 0;
}

public interface IAvailableStockPort
{
    AvailableStockResult Evaluate(IReadOnlyList<AvailableStockRequest> requests);
}

public class FailClosedAvailableStockPort : IAvailableStockPort
{
    public AvailableStockResult Evaluate(IReadOnlyList<AvailableStockRequest> requests)
        => AvailableStockResult.FailClosed();
}

public class DeterministicAvailableStockPort : IAvailableStockPort
{
    private readonly Func<string, decimal> _qtyFor;

    public DeterministicAvailableStockPort(Func<string, decimal> qtyFor)
    {
        _qtyFor = qtyFor;
    }

    public static DeterministicAvailableStockPort Full()
        => new(_ => decimal.MaxValue);

    public static DeterministicAvailableStockPort Zero()
        => new(_ => 0);

    public static DeterministicAvailableStockPort Partial(decimal qty)
        => new(_ => qty);

    public AvailableStockResult Evaluate(IReadOnlyList<AvailableStockRequest> requests)
        => new(true, "", requests.Select(x => new AvailableStockLine(x.BrgId, _qtyFor(x.BrgId))).ToList());
}

public class AvailableStockUnavailableException : ApotekDomainException
{
    public AvailableStockUnavailableException()
        : base("Available Stock evaluator is not configured (PD-09). Sales Order was not committed.")
    {
    }
}
