using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.SalesOrderFeature;

public class SalesOrderModel : ISalesOrderKey
{
    public const string IdPrefix = "ASO";
    private readonly List<SalesOrderItemModel> _items;
    private readonly List<SalesOrderItemComponentModel> _components;
    private readonly List<UnfulfilledOutcomeModel> _outcomes;

    private SalesOrderModel(
        string salesOrderId,
        SalesOrderSourceKindEnum sourceKind,
        string sourceId,
        string telaahResepId,
        string regId,
        string pasienId,
        string pasienName,
        PayerPathEnum payerPath,
        PartialReasonEnum partialReason,
        SalesOrderStatusEnum salesOrderStatus,
        SalesOrderResolvedReasonEnum resolvedReason,
        int version,
        IEnumerable<SalesOrderItemModel> items,
        IEnumerable<SalesOrderItemComponentModel> components,
        IEnumerable<UnfulfilledOutcomeModel> outcomes)
    {
        SalesOrderId = salesOrderId;
        SourceKind = sourceKind;
        SourceId = sourceId;
        TelaahResepId = telaahResepId ?? "";
        RegId = regId;
        PasienId = pasienId;
        PasienName = pasienName;
        PayerPath = payerPath;
        PartialReason = partialReason;
        SalesOrderStatus = salesOrderStatus;
        ResolvedReason = resolvedReason;
        Version = version;
        _items = items.ToList();
        _components = components.ToList();
        _outcomes = outcomes.ToList();
    }

    public static ISalesOrderKey Key(string salesOrderId) => new SalesOrderKey(salesOrderId);

    public static SalesOrderModel Establish(
        SalesOrderSourceKindEnum sourceKind,
        string sourceId,
        string telaahResepId,
        string regId,
        string pasienId,
        string pasienName,
        PayerPathEnum payerPath,
        PartialReasonEnum partialReason,
        IEnumerable<SalesOrderItemModel> items,
        IEnumerable<SalesOrderItemComponentModel> components)
    {
        Guard.Against.NullOrWhiteSpace(sourceId, nameof(sourceId));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));
        if (sourceKind == SalesOrderSourceKindEnum.ResepKerja && string.IsNullOrWhiteSpace(telaahResepId))
            throw new ApotekDomainException("Prescription Sales Order requires terminal Telaah.");
        if (sourceKind == SalesOrderSourceKindEnum.JualBebas && !string.IsNullOrWhiteSpace(telaahResepId))
            throw new ApotekDomainException("Jual Bebas Sales Order must not carry Telaah.");
        var list = items.ToList();
        if (list.Count == 0 || list.All(x => x.AcceptedQty <= 0))
            throw new ApotekDomainException("Sales Order requires at least one item with positive Accepted Qty.");

        return new SalesOrderModel(
            NunaId.New(IdPrefix),
            sourceKind,
            sourceId,
            telaahResepId ?? "",
            regId,
            pasienId,
            pasienName,
            payerPath,
            partialReason,
            SalesOrderStatusEnum.Established,
            SalesOrderResolvedReasonEnum.None,
            version: 1,
            list,
            components,
            []);
    }

    public static SalesOrderModel Rehydrate(
        string salesOrderId,
        SalesOrderSourceKindEnum sourceKind,
        string sourceId,
        string telaahResepId,
        string regId,
        string pasienId,
        string pasienName,
        PayerPathEnum payerPath,
        PartialReasonEnum partialReason,
        SalesOrderStatusEnum salesOrderStatus,
        SalesOrderResolvedReasonEnum resolvedReason,
        int version,
        IEnumerable<SalesOrderItemModel> items,
        IEnumerable<SalesOrderItemComponentModel> components,
        IEnumerable<UnfulfilledOutcomeModel> outcomes)
        => new(salesOrderId, sourceKind, sourceId, telaahResepId, regId, pasienId, pasienName,
            payerPath, partialReason, salesOrderStatus, resolvedReason, version, items, components, outcomes);

    public bool IsActiveKey =>
        SalesOrderStatus is SalesOrderStatusEnum.Established or SalesOrderStatusEnum.Active;

    public void Activate()
    {
        if (SalesOrderStatus != SalesOrderStatusEnum.Established)
            throw new ApotekDomainException("Only an Established Sales Order can become Active.");
        SalesOrderStatus = SalesOrderStatusEnum.Active;
        Version++;
    }

    public void ApplyInvoiceQty(int itemNo, decimal qty)
    {
        Item(itemNo).ApplyInvoice(qty);
        Version++;
    }

    public void ApplyDispenseQty(int itemNo, decimal qty)
    {
        Item(itemNo).ApplyDispense(qty);
        Version++;
    }

    public UnfulfilledOutcomeModel AppendUnfulfilled(
        int itemNo,
        decimal qty,
        UnfulfilledReasonEnum reason,
        string copyResepId,
        string actorId,
        DateTime effectiveAt)
    {
        var item = Item(itemNo);
        if (qty > item.UnresolvedAcceptedQty)
            throw new ApotekDomainException("Unfulfilled qty exceeds unresolved Accepted Qty.");
        item.ApplyUnfulfilled(qty);
        var outcome = new UnfulfilledOutcomeModel(
            _outcomes.Count == 0 ? 1 : _outcomes.Max(x => x.OutcomeNo) + 1,
            itemNo,
            qty,
            reason,
            copyResepId,
            actorId,
            effectiveAt);
        _outcomes.Add(outcome);
        Version++;
        return outcome;
    }

    public void SnapshotItemCoverage(int itemNo, FornasCoverageEnum coverage, string sepNo)
    {
        Item(itemNo).SnapshotCoverage(coverage, sepNo);
        Version++;
    }

    public void Resolve(SalesOrderResolvedReasonEnum reason)
    {
        if (!IsActiveKey)
            throw new ApotekDomainException("Only Established or Active Sales Order can resolve.");
        SalesOrderStatus = SalesOrderStatusEnum.Resolved;
        ResolvedReason = reason;
        Version++;
    }

    public void Cancel()
    {
        if (!IsActiveKey)
            throw new ApotekDomainException("Only Established or Active Sales Order can cancel.");
        SalesOrderStatus = SalesOrderStatusEnum.Cancelled;
        ResolvedReason = SalesOrderResolvedReasonEnum.Cancelled;
        Version++;
    }

    public void AssertExpectedVersion(int expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ApotekConcurrencyException(SalesOrderId, expectedVersion);
    }

    public SalesOrderItemModel Item(int itemNo)
        => _items.FirstOrDefault(x => x.ItemNo == itemNo)
           ?? throw new ApotekDomainException($"Sales Order item {itemNo} not found.");

    public string SalesOrderId { get; }
    public SalesOrderSourceKindEnum SourceKind { get; }
    public string SourceId { get; }
    public string TelaahResepId { get; }
    public string RegId { get; }
    public string PasienId { get; }
    public string PasienName { get; }
    public PayerPathEnum PayerPath { get; }
    public PartialReasonEnum PartialReason { get; }
    public SalesOrderStatusEnum SalesOrderStatus { get; private set; }
    public SalesOrderResolvedReasonEnum ResolvedReason { get; private set; }
    public int Version { get; private set; }
    public IReadOnlyList<SalesOrderItemModel> Items => _items;
    public IReadOnlyList<SalesOrderItemComponentModel> Components => _components;
    public IReadOnlyList<UnfulfilledOutcomeModel> Outcomes => _outcomes;

    private sealed record SalesOrderKey(string SalesOrderId) : ISalesOrderKey;
}
