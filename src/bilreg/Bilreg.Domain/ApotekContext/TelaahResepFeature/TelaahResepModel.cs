using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.TelaahResepFeature;

public class TelaahResepModel : ITelaahResepKey
{
    public const string IdPrefix = "ATR";
    private readonly List<TelaahResepItemModel> _items;

    private TelaahResepModel(
        string telaahResepId,
        string resepKerjaId,
        string regId,
        TelaahStatusEnum telaahStatus,
        string pharmacistId,
        DateTime startedAt,
        DateTime completedAt,
        int version,
        IEnumerable<TelaahResepItemModel> items)
    {
        TelaahResepId = telaahResepId;
        ResepKerjaId = resepKerjaId;
        RegId = regId;
        TelaahStatus = telaahStatus;
        PharmacistId = pharmacistId;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Version = version;
        _items = items.ToList();
    }

    public static ITelaahResepKey Key(string telaahResepId) => new TelaahResepKey(telaahResepId);

    public static TelaahResepModel Open(
        string resepKerjaId,
        string regId,
        IEnumerable<TelaahResepItemModel> pendingItems)
    {
        Guard.Against.NullOrWhiteSpace(resepKerjaId, nameof(resepKerjaId));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        var items = pendingItems.ToList();
        if (items.Count == 0)
            throw new ApotekDomainException("Telaah requires review items.");

        return new TelaahResepModel(
            NunaId.New(IdPrefix),
            resepKerjaId,
            regId,
            TelaahStatusEnum.Available,
            pharmacistId: "",
            startedAt: ApotekDate.Empty,
            completedAt: ApotekDate.Empty,
            version: 1,
            items);
    }

    public static TelaahResepModel Rehydrate(
        string telaahResepId,
        string resepKerjaId,
        string regId,
        TelaahStatusEnum telaahStatus,
        string pharmacistId,
        DateTime startedAt,
        DateTime completedAt,
        int version,
        IEnumerable<TelaahResepItemModel> items)
        => new(telaahResepId, resepKerjaId, regId, telaahStatus, pharmacistId, startedAt, completedAt, version, items);

    public bool IsTerminal =>
        TelaahStatus is TelaahStatusEnum.Approved or TelaahStatusEnum.PartiallyApproved or TelaahStatusEnum.Rejected;

    public bool CanEstablishSalesOrder =>
        TelaahStatus is TelaahStatusEnum.Approved or TelaahStatusEnum.PartiallyApproved;

    public void Start(string pharmacistId, DateTime startedAt)
    {
        Guard.Against.NullOrWhiteSpace(pharmacistId, nameof(pharmacistId));
        if (TelaahStatus != TelaahStatusEnum.Available)
            throw new ApotekDomainException("Telaah can start only from Available.");
        PharmacistId = pharmacistId;
        StartedAt = startedAt;
        TelaahStatus = TelaahStatusEnum.UnderReview;
        Version++;
    }

    public void UpdateItem(TelaahResepItemModel item)
    {
        EnsureUnderReview();
        var idx = _items.FindIndex(x => x.ItemNo == item.ItemNo);
        if (idx < 0)
            throw new ApotekDomainException($"Telaah item {item.ItemNo} does not exist.");
        _items[idx] = item;
        Version++;
    }

    public void Complete(DateTime completedAt)
    {
        EnsureUnderReview();
        if (_items.Any(x => !x.IsTerminal))
            throw new ApotekDomainException("Every line must have an explicit disposition before completion.");

        var accepted = _items.Count(x => x.IsAccepted);
        TelaahStatus = accepted == 0
            ? TelaahStatusEnum.Rejected
            : accepted == _items.Count
                ? TelaahStatusEnum.Approved
                : TelaahStatusEnum.PartiallyApproved;
        CompletedAt = completedAt;
        Version++;
    }

    public IReadOnlyList<TelaahResepItemModel> AcceptedItems()
        => _items.Where(x => x.IsAccepted).ToList();

    public void AssertExpectedVersion(int expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ApotekConcurrencyException(TelaahResepId, expectedVersion);
    }

    private void EnsureUnderReview()
    {
        if (TelaahStatus != TelaahStatusEnum.UnderReview)
            throw new ApotekDomainException("Telaah items can change only while Under Review.");
    }

    public string TelaahResepId { get; }
    public string ResepKerjaId { get; }
    public string RegId { get; }
    public TelaahStatusEnum TelaahStatus { get; private set; }
    public string PharmacistId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public int Version { get; private set; }
    public IReadOnlyList<TelaahResepItemModel> Items => _items;

    private sealed record TelaahResepKey(string TelaahResepId) : ITelaahResepKey;
}
