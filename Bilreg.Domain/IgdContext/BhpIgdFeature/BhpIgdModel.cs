using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.IgdContext.BhpIgdFeature;

public class BhpIgdModel : IBhpIgdKey
{
    private const string ID_PREFIX = "BHI";

    #region CREATION
    public BhpIgdModel(
        string bhpIgdId,
        string igdVisitId,
        string regId,
        string bhpItemId,
        string bhpItemName,
        int qty,
        decimal price,
        AuditInfoType audit)
    {
        BhpIgdId = bhpIgdId;
        IgdVisitId = igdVisitId;
        RegId = regId;
        BhpItemId = bhpItemId;
        BhpItemName = bhpItemName;
        Qty = qty;
        Price = price;
        Audit = audit;
    }

    public static BhpIgdModel Default => new(
        bhpIgdId: "-",
        igdVisitId: "-",
        regId: "-",
        bhpItemId: "-",
        bhpItemName: "-",
        qty: 0,
        price: 0,
        audit: AuditInfoType.Default);

    public static IBhpIgdKey Key(string id) => new BhpIgdModel(
        bhpIgdId: id,
        igdVisitId: "-",
        regId: "-",
        bhpItemId: "-",
        bhpItemName: "-",
        qty: 0,
        price: 0,
        audit: AuditInfoType.Default);

    public static BhpIgdModel Create(
        IgdVisitModel visit,
        string bhpItemId, string bhpItemName, int qty, decimal price,
        AuditInfoType audit)
    {
        Guard.Against.Null(visit);
        Guard.Against.NullOrWhiteSpace(bhpItemId, nameof(bhpItemId));
        Guard.Against.NegativeOrZero(qty, nameof(qty));
        Guard.Against.Negative(price, nameof(price));
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        if (visit.IsTerminal)
            throw new InvalidOperationException(
                $"Visit {visit.IgdVisitId} sudah terminal; BHP tidak dapat ditambahkan.");
        if (!visit.HasReg)
            throw new InvalidOperationException(
                $"Visit {visit.IgdVisitId} belum memiliki RegId; BHP tidak dapat ditambahkan (rule 7.3).");

        return new BhpIgdModel(
            bhpIgdId: NunaId.New(ID_PREFIX),
            igdVisitId: visit.IgdVisitId,
            regId: visit.Reg.RegId,
            bhpItemId: bhpItemId,
            bhpItemName: string.IsNullOrWhiteSpace(bhpItemName) ? "-" : bhpItemName,
            qty: qty,
            price: price,
            audit: audit);
    }
    #endregion

    #region PROPERTIES
    public string BhpIgdId { get; init; }
    public string IgdVisitId { get; init; }
    public string RegId { get; init; }
    public string BhpItemId { get; init; }
    public string BhpItemName { get; init; }
    public int Qty { get; init; }
    public decimal Price { get; init; }
    public AuditInfoType Audit { get; init; }
    public decimal Subtotal => Qty * Price;
    #endregion
}
