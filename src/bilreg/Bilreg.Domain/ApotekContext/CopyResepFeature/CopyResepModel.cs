using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.CopyResepFeature;

public interface ICopyResepKey
{
    string CopyResepId { get; }
}

public class CopyResepItemModel
{
    public CopyResepItemModel(int itemNo, int resepKerjaItemNo, string brgId, decimal qty, string note)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        if (qty <= 0)
            throw new ApotekDomainException("Copy Resep qty must be greater than zero.");
        ItemNo = itemNo;
        ResepKerjaItemNo = resepKerjaItemNo;
        BrgId = brgId;
        Qty = qty;
        Note = note ?? "";
    }

    public int ItemNo { get; }
    public int ResepKerjaItemNo { get; }
    public string BrgId { get; }
    public decimal Qty { get; }
    public string Note { get; }
}

public class CopyResepModel : ICopyResepKey
{
    public const string IdPrefix = "ACR";
    private readonly List<CopyResepItemModel> _items;

    private CopyResepModel(
        string copyResepId,
        string resepKerjaId,
        string salesOrderId,
        int reason,
        string issuedBy,
        DateTime issuedAt,
        IEnumerable<CopyResepItemModel> items)
    {
        CopyResepId = copyResepId;
        ResepKerjaId = resepKerjaId ?? "";
        SalesOrderId = salesOrderId ?? "";
        Reason = reason;
        IssuedBy = issuedBy;
        IssuedAt = issuedAt;
        _items = items.ToList();
    }

    public static ICopyResepKey Key(string copyResepId) => new CopyResepKey(copyResepId);

    public static CopyResepModel Issue(
        string resepKerjaId,
        string salesOrderId,
        int reason,
        string issuedBy,
        DateTime issuedAt,
        IEnumerable<CopyResepItemModel> items)
    {
        Guard.Against.NullOrWhiteSpace(issuedBy, nameof(issuedBy));
        var list = items.ToList();
        if (list.Count == 0)
            throw new ApotekDomainException("Copy Resep requires excluded quantities.");
        return new CopyResepModel(NunaId.New(IdPrefix), resepKerjaId, salesOrderId, reason, issuedBy, issuedAt, list);
    }

    public static CopyResepModel Rehydrate(
        string copyResepId,
        string resepKerjaId,
        string salesOrderId,
        int reason,
        string issuedBy,
        DateTime issuedAt,
        IEnumerable<CopyResepItemModel> items)
        => new(copyResepId, resepKerjaId, salesOrderId, reason, issuedBy, issuedAt, items);

    public string CopyResepId { get; }
    public string ResepKerjaId { get; }
    public string SalesOrderId { get; }
    public int Reason { get; }
    public string IssuedBy { get; }
    public DateTime IssuedAt { get; }
    public IReadOnlyList<CopyResepItemModel> Items => _items;

    private sealed record CopyResepKey(string CopyResepId) : ICopyResepKey;
}
