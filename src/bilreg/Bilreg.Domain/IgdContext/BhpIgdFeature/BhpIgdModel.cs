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
        AuditInfoType audit,
        string trsBillId = "",
        bool isPosted = false,
        DateTime? postedDate = null)
    {
        BhpIgdId = bhpIgdId;
        IgdVisitId = igdVisitId;
        RegId = regId;
        BhpItemId = bhpItemId;
        BhpItemName = bhpItemName;
        Qty = qty;
        Price = price;
        Audit = audit;
        TrsBillId = trsBillId ?? string.Empty;
        IsPosted = isPosted;
        PostedDate = postedDate;
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

        var regId = visit.HasReg ? visit.Reg.RegId : "-";

        return new BhpIgdModel(
            bhpIgdId: NunaId.New(ID_PREFIX),
            igdVisitId: visit.IgdVisitId,
            regId: regId,
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
    public string RegId { get; private set; }
    public string BhpItemId { get; init; }
    public string BhpItemName { get; init; }
    public int Qty { get; init; }
    public decimal Price { get; init; }
    public AuditInfoType Audit { get; init; }
    public string TrsBillId { get; private set; }
    public bool IsPosted { get; private set; }
    public DateTime? PostedDate { get; private set; }
    public decimal Subtotal => Qty * Price;
    #endregion

    #region BEHAVIOUR
    public void AssignRegister(string regId)
    {
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        RegId = regId;
    }

    public void MarkAsPosted(string trsBillId, DateTime postedAt)
    {
        Guard.Against.NullOrWhiteSpace(trsBillId, nameof(trsBillId));
        TrsBillId = trsBillId;
        IsPosted = true;
        PostedDate = postedAt;
    }
    #endregion
}
