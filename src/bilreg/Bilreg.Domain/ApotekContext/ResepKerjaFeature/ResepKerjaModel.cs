using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.ResepKerjaFeature;

public class ResepKerjaModel : IResepKerjaKey
{
    public const string IdPrefix = "ARX";
    private readonly List<ResepKerjaItemModel> _items;
    private readonly List<ResepKerjaComponentModel> _components;

    private ResepKerjaModel(
        string resepKerjaId,
        ResepKerjaSourceKindEnum sourceKind,
        string sourceResepId,
        string regId,
        string pasienId,
        string pasienName,
        string dokterId,
        string dokterName,
        string layananId,
        int urgenitas,
        int iterEntitled,
        int iterConsumed,
        int careSetting,
        string captureNote,
        string documentRef,
        ResepKerjaStatusEnum resepKerjaStatus,
        bool itemsFrozen,
        AuditTrailType auditTrail,
        IEnumerable<ResepKerjaItemModel> items,
        IEnumerable<ResepKerjaComponentModel> components)
    {
        ResepKerjaId = resepKerjaId;
        SourceKind = sourceKind;
        SourceResepId = sourceResepId ?? "";
        RegId = regId;
        PasienId = pasienId;
        PasienName = pasienName;
        DokterId = dokterId ?? "";
        DokterName = dokterName ?? "";
        LayananId = layananId ?? "";
        Urgenitas = urgenitas;
        IterEntitled = iterEntitled;
        IterConsumed = iterConsumed;
        CareSetting = careSetting;
        CaptureNote = captureNote ?? "";
        DocumentRef = documentRef ?? "";
        ResepKerjaStatus = resepKerjaStatus;
        ItemsFrozen = itemsFrozen;
        AuditTrail = auditTrail;
        _items = items.ToList();
        _components = components.ToList();
    }

    public static IResepKerjaKey Key(string resepKerjaId)
        => new ResepKerjaKey(resepKerjaId);

    public static ResepKerjaModel IntakeElectronic(
        ResepKerjaSourceKindEnum sourceKind,
        string sourceResepId,
        string regId,
        string pasienId,
        string pasienName,
        string dokterId,
        string dokterName,
        string layananId,
        int urgenitas,
        int iterEntitled,
        IEnumerable<ResepKerjaItemModel> items,
        IEnumerable<ResepKerjaComponentModel> components,
        AuditTrailType audit)
    {
        if (sourceKind == ResepKerjaSourceKindEnum.Physical)
            throw new ApotekDomainException("Electronic intake cannot use Physical source kind.");
        Guard.Against.NullOrWhiteSpace(sourceResepId, nameof(sourceResepId));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));
        Guard.Against.NullOrWhiteSpace(pasienName, nameof(pasienName));
        var list = items.ToList();
        if (list.Count == 0)
            throw new ApotekDomainException("Resep Kerja requires at least one item.");

        return new ResepKerjaModel(
            NunaId.New(IdPrefix),
            sourceKind,
            sourceResepId,
            regId,
            pasienId,
            pasienName,
            dokterId,
            dokterName,
            layananId,
            urgenitas,
            iterEntitled,
            iterConsumed: 0,
            careSetting: 0,
            captureNote: "",
            documentRef: "",
            ResepKerjaStatusEnum.Active,
            itemsFrozen: false,
            audit,
            list,
            components);
    }

    public static ResepKerjaModel IntakePhysical(
        string regId,
        string pasienId,
        string pasienName,
        string dokterId,
        string dokterName,
        string layananId,
        string captureNote,
        string documentRef,
        IEnumerable<ResepKerjaItemModel> items,
        IEnumerable<ResepKerjaComponentModel> components,
        AuditTrailType audit)
    {
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));
        Guard.Against.NullOrWhiteSpace(pasienName, nameof(pasienName));
        Guard.Against.NullOrWhiteSpace(captureNote, nameof(captureNote));
        Guard.Against.NullOrWhiteSpace(documentRef, nameof(documentRef));
        var list = items.ToList();
        if (list.Count == 0)
            throw new ApotekDomainException("Physical Resep Kerja requires at least one item.");

        return new ResepKerjaModel(
            NunaId.New(IdPrefix),
            ResepKerjaSourceKindEnum.Physical,
            sourceResepId: "",
            regId,
            pasienId,
            pasienName,
            dokterId,
            dokterName,
            layananId,
            urgenitas: 0,
            iterEntitled: 0,
            iterConsumed: 0,
            careSetting: 0,
            captureNote,
            documentRef,
            ResepKerjaStatusEnum.Active,
            itemsFrozen: false,
            audit,
            list,
            components);
    }

    public static ResepKerjaModel Rehydrate(
        string resepKerjaId,
        ResepKerjaSourceKindEnum sourceKind,
        string sourceResepId,
        string regId,
        string pasienId,
        string pasienName,
        string dokterId,
        string dokterName,
        string layananId,
        int urgenitas,
        int iterEntitled,
        int iterConsumed,
        int careSetting,
        string captureNote,
        string documentRef,
        ResepKerjaStatusEnum resepKerjaStatus,
        bool itemsFrozen,
        AuditTrailType auditTrail,
        IEnumerable<ResepKerjaItemModel> items,
        IEnumerable<ResepKerjaComponentModel> components)
        => new(
            resepKerjaId,
            sourceKind,
            sourceResepId,
            regId,
            pasienId,
            pasienName,
            dokterId,
            dokterName,
            layananId,
            urgenitas,
            iterEntitled,
            iterConsumed,
            careSetting,
            captureNote,
            documentRef,
            resepKerjaStatus,
            itemsFrozen,
            auditTrail,
            items,
            components);

    public void RewriteItems(
        IEnumerable<ResepKerjaItemModel> items,
        IEnumerable<ResepKerjaComponentModel> components,
        AuditTrailType audit)
    {
        EnsureActive();
        if (ItemsFrozen)
            throw new ApotekDomainException("Resep Kerja items are frozen after terminal Telaah.");
        var list = items.ToList();
        if (list.Count == 0)
            throw new ApotekDomainException("Resep Kerja requires at least one item.");
        _items.Clear();
        _items.AddRange(list);
        _components.Clear();
        _components.AddRange(components);
        AuditTrail.Modif(audit.Modified.UserId.Length == 0 ? audit.Created.UserId : audit.Modified.UserId,
            audit.Modified.Timestamp == ApotekDate.Empty ? DateTime.Now : audit.Modified.Timestamp);
    }

    public void FreezeItems() => ItemsFrozen = true;

    public void RecordIterConsumedVisibleCopy(int consumed)
    {
        if (consumed < 0)
            throw new ArgumentOutOfRangeException(nameof(consumed));
        IterConsumed = consumed;
    }

    public void Void(string userId, DateTime at)
    {
        EnsureActive();
        AuditTrail.Batal(userId, at);
        ResepKerjaStatus = ResepKerjaStatusEnum.Voided;
    }

    private void EnsureActive()
    {
        if (ResepKerjaStatus != ResepKerjaStatusEnum.Active)
            throw new ApotekDomainException("Resep Kerja is not active.");
    }

    public string ResepKerjaId { get; }
    public ResepKerjaSourceKindEnum SourceKind { get; }
    public string SourceResepId { get; }
    public string RegId { get; }
    public string PasienId { get; }
    public string PasienName { get; }
    public string DokterId { get; }
    public string DokterName { get; }
    public string LayananId { get; }
    public int Urgenitas { get; }
    public int IterEntitled { get; }
    public int IterConsumed { get; private set; }
    public int CareSetting { get; }
    public string CaptureNote { get; }
    public string DocumentRef { get; }
    public ResepKerjaStatusEnum ResepKerjaStatus { get; private set; }
    public bool ItemsFrozen { get; private set; }
    public AuditTrailType AuditTrail { get; }
    public IReadOnlyList<ResepKerjaItemModel> Items => _items;
    public IReadOnlyList<ResepKerjaComponentModel> Components => _components;

    private sealed record ResepKerjaKey(string ResepKerjaId) : IResepKerjaKey;
}
