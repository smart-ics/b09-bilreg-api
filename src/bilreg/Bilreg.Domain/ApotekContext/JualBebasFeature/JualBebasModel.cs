using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.JualBebasFeature;

public class JualBebasModel : IJualBebasKey
{
    public const string IdPrefix = "ADQ";
    private readonly List<JualBebasItemModel> _items;

    private JualBebasModel(
        string jualBebasId,
        string regId,
        string pasienId,
        string pasienName,
        string acceptedBy,
        DateTime acceptedAt,
        JualBebasRequestStatusEnum requestStatus,
        IEnumerable<JualBebasItemModel> items)
    {
        JualBebasId = jualBebasId;
        RegId = regId;
        PasienId = pasienId;
        PasienName = pasienName;
        AcceptedBy = acceptedBy;
        AcceptedAt = acceptedAt;
        RequestStatus = requestStatus;
        _items = items.ToList();
    }

    public static IJualBebasKey Key(string jualBebasId) => new JualBebasKey(jualBebasId);

    public static JualBebasModel Accept(
        string regId,
        string pasienId,
        string pasienName,
        string acceptedBy,
        DateTime acceptedAt,
        IEnumerable<JualBebasItemModel> items)
    {
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));
        Guard.Against.NullOrWhiteSpace(pasienName, nameof(pasienName));
        Guard.Against.NullOrWhiteSpace(acceptedBy, nameof(acceptedBy));
        var list = items.ToList();
        if (list.Count == 0)
            throw new ApotekDomainException("Jual Bebas requires catalog-backed items.");

        return new JualBebasModel(
            NunaId.New(IdPrefix),
            regId,
            pasienId,
            pasienName,
            acceptedBy,
            acceptedAt,
            JualBebasRequestStatusEnum.Accepted,
            list);
    }

    public static JualBebasModel Rehydrate(
        string jualBebasId,
        string regId,
        string pasienId,
        string pasienName,
        string acceptedBy,
        DateTime acceptedAt,
        JualBebasRequestStatusEnum requestStatus,
        IEnumerable<JualBebasItemModel> items)
        => new(jualBebasId, regId, pasienId, pasienName, acceptedBy, acceptedAt, requestStatus, items);

    public void MarkConvertedToSalesOrder()
    {
        if (RequestStatus != JualBebasRequestStatusEnum.Accepted)
            throw new ApotekDomainException("Only an accepted Jual Bebas can convert to a Sales Order.");
        RequestStatus = JualBebasRequestStatusEnum.ConvertedToSalesOrder;
    }

    public void DeclineAfterAccept(string actorId)
    {
        Guard.Against.NullOrWhiteSpace(actorId, nameof(actorId));
        if (RequestStatus != JualBebasRequestStatusEnum.Accepted)
            throw new ApotekDomainException("Only an accepted Jual Bebas can be cancelled after accept.");
        RequestStatus = JualBebasRequestStatusEnum.DeclinedAfterAccept;
    }

    public string JualBebasId { get; }
    public string RegId { get; }
    public string PasienId { get; }
    public string PasienName { get; }
    public string AcceptedBy { get; }
    public DateTime AcceptedAt { get; }
    public JualBebasRequestStatusEnum RequestStatus { get; private set; }
    public IReadOnlyList<JualBebasItemModel> Items => _items;

    private sealed record JualBebasKey(string JualBebasId) : IJualBebasKey;
}
