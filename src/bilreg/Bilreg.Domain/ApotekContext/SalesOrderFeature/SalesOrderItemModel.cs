using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Domain.ApotekContext.SalesOrderFeature;

public class SalesOrderItemModel
{
    public SalesOrderItemModel(
        int itemNo,
        int sourceItemNo,
        string brgId,
        string brgName,
        string satuanId,
        decimal acceptedQty,
        decimal invoicedQty,
        decimal dispensedQty,
        decimal unfulfilledQty,
        SalesOrderItemStatusEnum itemStatus,
        FornasCoverageEnum fornasCoverage,
        string sepNo,
        bool isRacik)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        if (acceptedQty <= 0)
            throw new ApotekDomainException("Accepted Qty must be greater than zero.");
        ItemNo = itemNo;
        SourceItemNo = sourceItemNo;
        BrgId = brgId;
        BrgName = brgName ?? "";
        SatuanId = satuanId ?? "";
        AcceptedQty = acceptedQty;
        InvoicedQty = invoicedQty;
        DispensedQty = dispensedQty;
        UnfulfilledQty = unfulfilledQty;
        ItemStatus = itemStatus;
        FornasCoverage = fornasCoverage;
        SepNo = sepNo ?? "";
        IsRacik = isRacik;
        AssertQuantities();
    }

    public static SalesOrderItemModel Establish(
        int itemNo,
        int sourceItemNo,
        string brgId,
        string brgName,
        string satuanId,
        decimal acceptedQty,
        FornasCoverageEnum fornasCoverage,
        string sepNo,
        bool isRacik)
        => new(itemNo, sourceItemNo, brgId, brgName, satuanId, acceptedQty, 0, 0, 0,
            SalesOrderItemStatusEnum.Active, fornasCoverage, sepNo, isRacik);

    public decimal UnresolvedAcceptedQty => AcceptedQty - DispensedQty - UnfulfilledQty;

    public void ApplyInvoice(decimal qty)
    {
        if (qty < 0 || InvoicedQty + qty > AcceptedQty)
            throw new ApotekDomainException("Invoiced, dispensed, and unfulfilled qty cannot exceed Accepted Qty.");
        InvoicedQty += qty;
        if (InvoicedQty >= AcceptedQty)
            ItemStatus = SalesOrderItemStatusEnum.FullyInvoiced;
    }

    public void ApplyDispense(decimal qty)
    {
        if (qty < 0 || DispensedQty + qty > AcceptedQty || DispensedQty + qty + UnfulfilledQty > AcceptedQty)
            throw new ApotekDomainException("Invoiced, dispensed, and unfulfilled qty cannot exceed Accepted Qty.");
        DispensedQty += qty;
        if (DispensedQty >= AcceptedQty)
            ItemStatus = SalesOrderItemStatusEnum.FullyFulfilled;
    }

    public void ApplyUnfulfilled(decimal qty)
    {
        if (qty < 0 || UnfulfilledQty + qty > AcceptedQty || DispensedQty + UnfulfilledQty + qty > AcceptedQty)
            throw new ApotekDomainException("Invoiced, dispensed, and unfulfilled qty cannot exceed Accepted Qty.");
        UnfulfilledQty += qty;
        if (UnresolvedAcceptedQty <= 0)
            ItemStatus = SalesOrderItemStatusEnum.Unfulfilled;
    }

    public void SnapshotCoverage(FornasCoverageEnum coverage, string sepNo)
    {
        FornasCoverage = coverage;
        SepNo = sepNo ?? "";
    }

    private void AssertQuantities()
    {
        if (InvoicedQty < 0 || DispensedQty < 0 || UnfulfilledQty < 0)
            throw new ApotekDomainException("Quantities cannot be negative.");
        if (InvoicedQty > AcceptedQty || DispensedQty > AcceptedQty || UnfulfilledQty > AcceptedQty)
            throw new ApotekDomainException("Invoiced, dispensed, and unfulfilled qty cannot exceed Accepted Qty.");
        if (DispensedQty + UnfulfilledQty > AcceptedQty)
            throw new ApotekDomainException("Dispensed plus unfulfilled qty cannot exceed Accepted Qty.");
    }

    public int ItemNo { get; }
    public int SourceItemNo { get; }
    public string BrgId { get; }
    public string BrgName { get; }
    public string SatuanId { get; }
    public decimal AcceptedQty { get; }
    public decimal InvoicedQty { get; private set; }
    public decimal DispensedQty { get; private set; }
    public decimal UnfulfilledQty { get; private set; }
    public SalesOrderItemStatusEnum ItemStatus { get; private set; }
    public FornasCoverageEnum FornasCoverage { get; private set; }
    public string SepNo { get; private set; }
    public bool IsRacik { get; }
}

public class SalesOrderItemComponentModel
{
    public SalesOrderItemComponentModel(int itemNo, int componentNo, string brgId, string brgName, decimal qty)
    {
        ItemNo = itemNo;
        ComponentNo = componentNo;
        BrgId = brgId;
        BrgName = brgName ?? "";
        Qty = qty;
    }

    public int ItemNo { get; }
    public int ComponentNo { get; }
    public string BrgId { get; }
    public string BrgName { get; }
    public decimal Qty { get; }
}

public class UnfulfilledOutcomeModel
{
    public UnfulfilledOutcomeModel(
        int outcomeNo,
        int salesOrderItemNo,
        decimal qty,
        UnfulfilledReasonEnum reason,
        string copyResepId,
        string actorId,
        DateTime effectiveAt)
    {
        Guard.Against.NegativeOrZero(outcomeNo, nameof(outcomeNo));
        if (qty <= 0)
            throw new ApotekDomainException("Unfulfilled qty must be greater than zero.");
        Guard.Against.NullOrWhiteSpace(actorId, nameof(actorId));
        OutcomeNo = outcomeNo;
        SalesOrderItemNo = salesOrderItemNo;
        Qty = qty;
        Reason = reason;
        CopyResepId = copyResepId ?? "";
        ActorId = actorId;
        EffectiveAt = effectiveAt;
    }

    public int OutcomeNo { get; }
    public int SalesOrderItemNo { get; }
    public decimal Qty { get; }
    public UnfulfilledReasonEnum Reason { get; }
    public string CopyResepId { get; }
    public string ActorId { get; }
    public DateTime EffectiveAt { get; }
}
