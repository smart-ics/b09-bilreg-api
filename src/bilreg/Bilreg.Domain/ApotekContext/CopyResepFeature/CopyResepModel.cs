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
    public CopyResepItemModel(int itemNo, int resepItemNo, string brgId, decimal qty, string note)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        if (qty <= 0)
            throw new ApotekDomainException("Copy Resep qty must be greater than zero.");
        ItemNo = itemNo;
        ResepItemNo = resepItemNo;
        BrgId = brgId;
        Qty = qty;
        Note = note ?? "";
    }

    public int ItemNo { get; }
    public int ResepItemNo { get; }
    [Obsolete("Use ResepItemNo")]
    public int ResepKerjaItemNo => ResepItemNo;
    public string BrgId { get; }
    public decimal Qty { get; }
    public string Note { get; }
}

public class CopyResepModel : ICopyResepKey
{
    public const string IdPrefix = "ACR";
    private static readonly HashSet<string> GeneratedIds = new();
    private static readonly object IdLock = new();

    private static string GenerateUniqueId()
    {
        lock (IdLock)
        {
            for (int i = 0; i < 100; i++)
            {
                var id = NunaId.New(IdPrefix);
                if (GeneratedIds.Add(id))
                    return id;
            }
            return NunaId.New(IdPrefix);
        }
    }

    private readonly List<CopyResepItemModel> _items;

    private CopyResepModel(
        string copyResepId,
        string resepId,
        string salesOrderId,
        int reason,
        string issuedBy,
        DateTime issuedAt,
        IEnumerable<CopyResepItemModel> items)
    {
        CopyResepId = copyResepId;
        ResepId = resepId ?? "";
        SalesOrderId = salesOrderId ?? "";
        Reason = reason;
        IssuedBy = issuedBy;
        IssuedAt = issuedAt;
        _items = items.ToList();
    }

    public static ICopyResepKey Key(string copyResepId) => new CopyResepKey(copyResepId);

    public static CopyResepModel Issue(
        string resepId,
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
        return new CopyResepModel(GenerateUniqueId(), resepId, salesOrderId, reason, issuedBy, issuedAt, list);
    }

    public static CopyResepModel Rehydrate(
        string copyResepId,
        string resepId,
        string salesOrderId,
        int reason,
        string issuedBy,
        DateTime issuedAt,
        IEnumerable<CopyResepItemModel> items)
        => new(copyResepId, resepId, salesOrderId, reason, issuedBy, issuedAt, items);

    public string CopyResepId { get; }
    public string ResepId { get; }
    [Obsolete("Use ResepId")]
    public string ResepKerjaId => ResepId;
    public string SalesOrderId { get; }
    public int Reason { get; }
    public string IssuedBy { get; }
    public DateTime IssuedAt { get; }
    public IReadOnlyList<CopyResepItemModel> Items => _items;

    private sealed record CopyResepKey(string CopyResepId) : ICopyResepKey;
}
