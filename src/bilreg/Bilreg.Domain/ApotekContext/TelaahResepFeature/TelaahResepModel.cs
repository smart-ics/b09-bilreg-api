using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.TelaahResepFeature;

public class TelaahResepModel : ITelaahResepKey
{
    public const string IdPrefix = "ATR";
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

    private static readonly HashSet<string> CanonicalKajianCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "1.1", "1.2", "1.3", "1.4",
        "2.1", "2.2", "2.3", "2.4",
        "3.1", "3.2", "3.3", "3.4", "3.5"
    };

    private readonly List<TelaahResepItemModel> _items;
    private readonly List<TelaahResepKajianModel> _kajianItems;

    private TelaahResepModel(
        string telaahResepId,
        string resepId,
        string regId,
        TelaahStatusEnum telaahStatus,
        string pharmacistId,
        DateTime startedAt,
        DateTime completedAt,
        int version,
        IEnumerable<TelaahResepItemModel> items,
        IEnumerable<TelaahResepKajianModel>? kajianItems = null)
    {
        TelaahResepId = telaahResepId;
        ResepId = resepId;
        RegId = regId;
        TelaahStatus = telaahStatus;
        PharmacistId = pharmacistId;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Version = version;
        _items = items.ToList();
        _kajianItems = kajianItems?.ToList() ?? TelaahResepKajianModel.DefaultCatalog().ToList();
    }

    public static ITelaahResepKey Key(string telaahResepId) => new TelaahResepKey(telaahResepId);

    public static TelaahResepModel Open(
        string resepId,
        string regId,
        IEnumerable<TelaahResepItemModel> pendingItems,
        IEnumerable<TelaahResepKajianModel>? kajianItems = null)
    {
        Guard.Against.NullOrWhiteSpace(resepId, nameof(resepId));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        var items = pendingItems.ToList();
        if (items.Count == 0)
            throw new ApotekDomainException("Telaah requires review items.");

        return new TelaahResepModel(
            GenerateUniqueId(),
            resepId,
            regId,
            TelaahStatusEnum.Available,
            pharmacistId: "",
            startedAt: ApotekDate.Empty,
            completedAt: ApotekDate.Empty,
            version: 1,
            items,
            kajianItems ?? TelaahResepKajianModel.DefaultCatalog());
    }

    public static TelaahResepModel Rehydrate(
        string telaahResepId,
        string resepId,
        string regId,
        TelaahStatusEnum telaahStatus,
        string pharmacistId,
        DateTime startedAt,
        DateTime completedAt,
        int version,
        IEnumerable<TelaahResepItemModel> items,
        IEnumerable<TelaahResepKajianModel>? kajianItems = null)
        => new(telaahResepId, resepId, regId, telaahStatus, pharmacistId, startedAt, completedAt, version, items,
            kajianItems ?? TelaahResepKajianModel.DefaultCatalog());

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

    public void UpdateKajian(string kajianResepId, bool isYes, string catatanKajian)
    {
        EnsureUnderReview();
        Guard.Against.NullOrWhiteSpace(kajianResepId, nameof(kajianResepId));
        var idx = _kajianItems.FindIndex(x => string.Equals(x.KajianResepId, kajianResepId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            throw new ApotekDomainException($"Telaah kajian item '{kajianResepId}' does not exist.");
        _kajianItems[idx] = _kajianItems[idx].WithAnswer(isYes, catatanKajian);
        Version++;
    }

    public void UpdateKajianBatch(IEnumerable<(string kajianResepId, bool isYes, string catatanKajian)> updates)
    {
        EnsureUnderReview();
        Guard.Against.Null(updates, nameof(updates));
        foreach (var (kajianResepId, isYes, catatanKajian) in updates)
        {
            var idx = _kajianItems.FindIndex(x => string.Equals(x.KajianResepId, kajianResepId?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                _kajianItems[idx] = _kajianItems[idx].WithAnswer(isYes, catatanKajian);
            }
        }
        Version++;
    }

    public void Complete(DateTime completedAt)
    {
        EnsureUnderReview();

        var presentCodes = new HashSet<string>(_kajianItems.Select(x => x.KajianResepId), StringComparer.OrdinalIgnoreCase);
        var missingCodes = CanonicalKajianCodes.Where(code => !presentCodes.Contains(code)).ToList();
        if (missingCodes.Count > 0)
            throw new ApotekDomainException($"Telaah resep kajian incomplete. Missing codes: {string.Join(", ", missingCodes)}.");

        foreach (var kajian in _kajianItems)
        {
            if (kajian.RequiresNote && string.IsNullOrWhiteSpace(kajian.CatatanKajian))
                throw new ApotekDomainException($"Catatan kajian wajib diisi untuk poin {kajian.KajianResepId} ({kajian.KajianResepName}).");
        }

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
    public string ResepId { get; }
    [Obsolete("Use ResepId")]
    public string ResepKerjaId => ResepId;
    public string RegId { get; }
    public TelaahStatusEnum TelaahStatus { get; private set; }
    public string PharmacistId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public int Version { get; private set; }
    public IReadOnlyList<TelaahResepItemModel> Items => _items;
    public IReadOnlyList<TelaahResepKajianModel> KajianItems => _kajianItems;

    private sealed record TelaahResepKey(string TelaahResepId) : ITelaahResepKey;
}
