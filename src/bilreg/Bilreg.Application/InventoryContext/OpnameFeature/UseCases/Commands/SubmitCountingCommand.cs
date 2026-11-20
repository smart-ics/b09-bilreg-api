using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public interface IOpnameItemInfoProvider
{
    decimal GetHpp(string brgId);
    string GetBaseSatuan(string brgId);
}

public class DefaultOpnameItemInfoProvider : IOpnameItemInfoProvider
{
    public decimal GetHpp(string brgId) => 0m;
    public string GetBaseSatuan(string brgId) => string.Empty;
}

public record SubmitCountingCommand(
    string SessionId,
    string SubmittedBy,
    DateTime? SubmittedAt = null,
    IReadOnlyDictionary<string, decimal>? ItemHppMap = null) : IRequest<SubmitCountingResponse>;

public record SubmitCountingResponse(
    string SessionId,
    SessionStatusEnum Status,
    int TotalCountedItems,
    int TotalVarianceItems,
    bool HasVariance,
    decimal SurplusValuation,
    decimal DeficitValuation,
    decimal NetValuation);

public class SubmitCountingHandler : IRequestHandler<SubmitCountingCommand, SubmitCountingResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;
    private readonly ISystemQuantityReconstructionService _reconstructionService;
    private readonly IOpnameItemInfoProvider _itemInfoProvider;

    public SubmitCountingHandler(
        IStockOpnameSessionRepo sessionRepo,
        ISystemQuantityReconstructionService reconstructionService,
        IOpnameItemInfoProvider? itemInfoProvider = null)
    {
        _sessionRepo = sessionRepo;
        _reconstructionService = reconstructionService;
        _itemInfoProvider = itemInfoProvider ?? new DefaultOpnameItemInfoProvider();
    }

    public async Task<SubmitCountingResponse> Handle(
        SubmitCountingCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));
        Guard.Against.NullOrWhiteSpace(request.SubmittedBy, nameof(request.SubmittedBy));

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"Stock opname session with ID '{request.SessionId}' not found.");

        var session = maybe.Value;
        if (session.Status != SessionStatusEnum.Counting)
            throw new InvalidOperationException($"Cannot submit counting from status {session.Status}. Expected Counting.");

        var submittedAt = request.SubmittedAt ?? DateTime.Now;
        var countTime = session.CountedItems.Count > 0
            ? session.CountedItems.Max(c => c.LastCountTime)
            : submittedAt;
        if (countTime == default) countTime = submittedAt;

        IEnumerable<string>? filterBrgIds = session.ScopeMode == SessionScopeEnum.Partial
            ? session.ScopeItems.Select(s => s.BrgId).Distinct().ToList()
            : null;

        var systemBalances = await _reconstructionService.ReconstructBalancesAsync(
            session.LayananId,
            countTime,
            filterBrgIds,
            cancellationToken);

        decimal GetSystemBalance(string brgId, string batchNo, string expiredDate)
        {
            var key = new ItemTupleKey(brgId, batchNo, expiredDate);
            return systemBalances.TryGetValue(key, out var qty) ? qty : 0m;
        }

        decimal ResolveHpp(string brgId)
        {
            if (request.ItemHppMap != null && request.ItemHppMap.TryGetValue(brgId, out var hpp))
                return hpp;
            return _itemInfoProvider.GetHpp(brgId);
        }

        var countedTuples = new HashSet<ItemTupleKey>();
        var allVarianceItems = new List<StockOpnameVarianceItemModel>();

        foreach (var counted in session.CountedItems)
        {
            var tupleKey = new ItemTupleKey(counted.BrgId, counted.BatchNo, counted.ExpiredDate);
            countedTuples.Add(tupleKey);

            var systemQty = GetSystemBalance(counted.BrgId, counted.BatchNo, counted.ExpiredDate);
            var hpp = ResolveHpp(counted.BrgId);
            var varianceItemId = $"VAR{Guid.NewGuid():N}"[..16];

            var varianceItem = StockOpnameVarianceItemModel.Create(
                varianceItemId: varianceItemId,
                sessionId: session.SessionId,
                brgId: counted.BrgId,
                batchNo: counted.BatchNo,
                expiredDate: counted.ExpiredDate,
                baseSatuanId: counted.BaseSatuanId,
                countQty: counted.TotalCountQty,
                systemQty: systemQty,
                hpp: hpp);

            allVarianceItems.Add(varianceItem);
        }

        // BR-SO-007: Uncounted Items in Full Opname evaluated as Missing Stock
        if (session.ScopeMode == SessionScopeEnum.Full)
        {
            foreach (var kvp in systemBalances)
            {
                var tupleKey = kvp.Key;
                var systemQty = kvp.Value;

                if (!countedTuples.Contains(tupleKey) && systemQty != 0)
                {
                    var baseSatuanId = _itemInfoProvider.GetBaseSatuan(tupleKey.BrgId);
                    var hpp = ResolveHpp(tupleKey.BrgId);
                    var varianceItemId = $"VAR{Guid.NewGuid():N}"[..16];

                    var uncountedVariance = StockOpnameVarianceItemModel.Create(
                        varianceItemId: varianceItemId,
                        sessionId: session.SessionId,
                        brgId: tupleKey.BrgId,
                        batchNo: tupleKey.BatchNo,
                        expiredDate: tupleKey.ExpiredDate,
                        baseSatuanId: baseSatuanId,
                        countQty: 0m,
                        systemQty: systemQty,
                        hpp: hpp);

                    allVarianceItems.Add(uncountedVariance);
                }
            }
        }

        // BR-SO-009 & BR-SO-010: Filter items with actual variance (VarianceQty != 0)
        var persistentVariances = allVarianceItems.Where(v => v.VarianceQty != 0).ToList();
        var hasVariance = persistentVariances.Count > 0;

        session.SetVarianceItems(persistentVariances);
        session.SubmitCounting(hasVariance, request.SubmittedBy, submittedAt);

        _sessionRepo.SaveChanges(session);

        return new SubmitCountingResponse(
            session.SessionId,
            session.Status,
            session.CountedItems.Count,
            session.VarianceItems.Count,
            hasVariance,
            session.SurplusValuation,
            session.DeficitValuation,
            session.NetValuation);
    }
}
