using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record TarifPolicyType : ITarifPolicyKey
{
    private const string IdPrefix = "TPF";
    private readonly List<TarifVariantType> _variants;

    public TarifPolicyType(
        string tarifPolicyId,
        string policyNo,
        string policyName,
        DateTime effectiveDateInfo,
        string description,
        TarifPolicyStatus policyStatus,
        AuditTrailType auditTrail,
        IEnumerable<TarifVariantType> variants)
    {
        TarifPolicyId = tarifPolicyId;
        PolicyNo = policyNo;
        PolicyName = policyName;
        EffectiveDateInfo = effectiveDateInfo;
        Description = description;
        PolicyStatus = policyStatus;
        AuditTrail = auditTrail;
        _variants = variants?.OrderBy(x => x.ItemNo).ToList() ?? [];
    }

    #region CREATION

    public static TarifPolicyType Create(
        string policyNo,
        string policyName,
        DateTime effectiveDateInfo,
        string description,
        string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(policyNo);
        Guard.Against.NullOrWhiteSpace(policyName);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var now = DateTime.Now;
        return new TarifPolicyType(
            NunaId.New(IdPrefix),
            policyNo,
            policyName,
            effectiveDateInfo,
            description ?? "",
            TarifPolicyStatus.Draft,
            AuditTrailType.Create(auditUserId, now),
            []);
    }

    public static TarifPolicyType Default => new(
        "-", "", "", new DateTime(3000, 1, 1), "", TarifPolicyStatus.Draft,
        AuditTrailType.Default, []);

    public static ITarifPolicyKey Key(string id) => Default with { TarifPolicyId = id };

    #endregion

    #region PROPERTIES

    public string TarifPolicyId { get; init; }
    public string PolicyNo { get; init; }
    public string PolicyName { get; init; }
    public DateTime EffectiveDateInfo { get; init; }
    public string Description { get; init; }
    public TarifPolicyStatus PolicyStatus { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public IEnumerable<TarifVariantType> Variants => _variants;

    #endregion

    #region BEHAVIOUR

    public void EnsureEditable()
    {
        if (PolicyStatus is TarifPolicyStatus.Published or TarifPolicyStatus.Archived)
            throw new InvalidOperationException(
                $"TarifPolicy {TarifPolicyId} berstatus {PolicyStatus}; perubahan tidak diperbolehkan.");
    }

    public TarifPolicyType AddVariant(
        string tarifId,
        string kelasId,
        string tipeTarifId,
        decimal nilai,
        IEnumerable<TarifVariantKomponenType> komponenLines)
    {
        EnsureEditable();
        Guard.Against.NullOrWhiteSpace(tarifId);
        Guard.Against.NullOrWhiteSpace(kelasId);
        Guard.Against.NullOrWhiteSpace(tipeTarifId);

        if (HasVariant(tarifId, kelasId, tipeTarifId))
            throw new InvalidOperationException(
                $"Variant duplikat untuk kombinasi Tarif={tarifId}, Kelas={kelasId}, TipeTarif={tipeTarifId}.");

        var itemNo = NextItemNo();
        var variant = TarifVariantType.Create(
            TarifPolicyId, itemNo, tarifId, kelasId, tipeTarifId, nilai, komponenLines);

        var nextVariants = _variants.ToList();
        nextVariants.Add(variant);
        return CloneWithVariants(nextVariants);
    }

    public static TarifPolicyType CopyFrom(
        TarifPolicyType source,
        string newPolicyNo,
        string newPolicyName,
        string auditUserId)
    {
        Guard.Against.Null(source);
        Guard.Against.NullOrWhiteSpace(newPolicyNo);
        Guard.Against.NullOrWhiteSpace(newPolicyName);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var newPolicyId = NunaId.New(IdPrefix);
        var clonedVariants = source._variants
            .Select(v => TarifVariantType.Create(
                newPolicyId,
                v.ItemNo,
                v.TarifId,
                v.KelasId,
                v.TipeTarifId,
                v.Nilai,
                v.ListKomponen))
            .ToList();

        return new TarifPolicyType(
            newPolicyId,
            newPolicyNo,
            newPolicyName,
            source.EffectiveDateInfo,
            source.Description,
            TarifPolicyStatus.Draft,
            AuditTrailType.Create(auditUserId, DateTime.Now),
            clonedVariants);
    }

    public TarifPolicyType MassAdjust(decimal percentFactor, string auditUserId)
    {
        EnsureEditable();
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var adjusted = _variants.Select(v => v.WithMassAdjustedNilai(percentFactor)).ToList();
        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return new TarifPolicyType(
            TarifPolicyId,
            PolicyNo,
            PolicyName,
            EffectiveDateInfo,
            Description,
            PolicyStatus,
            audit,
            adjusted);
    }

    public TarifPolicyType MarkReviewed(string auditUserId)
    {
        Guard.Against.NullOrWhiteSpace(auditUserId);

        if (PolicyStatus != TarifPolicyStatus.Draft)
            throw new InvalidOperationException(
                $"TarifPolicy {TarifPolicyId} harus Draft untuk ditandai Reviewed (status saat ini: {PolicyStatus}).");

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return new TarifPolicyType(
            TarifPolicyId,
            PolicyNo,
            PolicyName,
            EffectiveDateInfo,
            Description,
            TarifPolicyStatus.Reviewed,
            audit,
            _variants);
    }

    public void ValidateForPublish()
    {
        if (PolicyStatus is not TarifPolicyStatus.Draft and not TarifPolicyStatus.Reviewed)
            throw new InvalidOperationException(
                $"TarifPolicy {TarifPolicyId} tidak dapat dipublish dari status {PolicyStatus}.");

        if (_variants.Count == 0)
            throw new InvalidOperationException(
                $"TarifPolicy {TarifPolicyId} tidak memiliki variant; publish tidak diperbolehkan.");

        foreach (var variant in _variants)
            variant.EnsureValidForPublish();
    }

    public void ValidateForRepublish()
    {
        if (PolicyStatus != TarifPolicyStatus.Published)
            throw new InvalidOperationException(
                $"TarifPolicy {TarifPolicyId} hanya dapat di-republish dari status Published (status saat ini: {PolicyStatus}).");

        if (_variants.Count == 0)
            throw new InvalidOperationException(
                $"TarifPolicy {TarifPolicyId} tidak memiliki variant; publish tidak diperbolehkan.");

        foreach (var variant in _variants)
            variant.EnsureValidForPublish();
    }

    public TarifPolicyType MarkPublished(string auditUserId)
    {
        ValidateForPublish();
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return new TarifPolicyType(
            TarifPolicyId,
            PolicyNo,
            PolicyName,
            EffectiveDateInfo,
            Description,
            TarifPolicyStatus.Published,
            audit,
            _variants);
    }

    public TarifPolicyType UpdateMetadata(
        string policyNo,
        string policyName,
        DateTime effectiveDateInfo,
        string description,
        string auditUserId)
    {
        EnsureEditable();
        Guard.Against.NullOrWhiteSpace(policyNo);
        Guard.Against.NullOrWhiteSpace(policyName);
        Guard.Against.NullOrWhiteSpace(auditUserId);

        var audit = AuditTrail;
        audit.Modif(auditUserId, DateTime.Now);
        return new TarifPolicyType(
            TarifPolicyId,
            policyNo,
            policyName,
            effectiveDateInfo,
            description ?? "",
            PolicyStatus,
            audit,
            _variants);
    }

    public TarifPolicyType UpdateVariant(
        int itemNo,
        string tarifId,
        string kelasId,
        string tipeTarifId,
        IEnumerable<TarifVariantKomponenType> komponenLines)
    {
        EnsureEditable();
        Guard.Against.NullOrWhiteSpace(tarifId);
        Guard.Against.NullOrWhiteSpace(kelasId);
        Guard.Against.NullOrWhiteSpace(tipeTarifId);

        var existing = _variants.FirstOrDefault(v => v.ItemNo == itemNo)
            ?? throw new KeyNotFoundException(
                $"Variant ItemNo {itemNo} tidak ditemukan pada TarifPolicy {TarifPolicyId}.");

        if (HasVariantExcept(itemNo, tarifId, kelasId, tipeTarifId))
            throw new InvalidOperationException(
                $"Variant duplikat untuk kombinasi Tarif={tarifId}, Kelas={kelasId}, TipeTarif={tipeTarifId}.");

        var updated = TarifVariantType
            .Create(TarifPolicyId, itemNo, tarifId, kelasId, tipeTarifId, existing.Nilai, komponenLines)
            .SetKomponenLines(komponenLines);

        var nextVariants = _variants
            .Select(v => v.ItemNo == itemNo ? updated : v)
            .ToList();
        return CloneWithVariants(nextVariants);
    }

    public TarifPolicyType RemoveVariant(int itemNo)
    {
        EnsureEditable();

        if (_variants.All(v => v.ItemNo != itemNo))
            throw new KeyNotFoundException(
                $"Variant ItemNo {itemNo} tidak ditemukan pada TarifPolicy {TarifPolicyId}.");

        var nextVariants = _variants.Where(v => v.ItemNo != itemNo).ToList();
        return CloneWithVariants(nextVariants);
    }

    #endregion

    #region HELPERS

    private bool HasVariantExcept(int itemNo, string tarifId, string kelasId, string tipeTarifId) =>
        _variants.Any(v =>
            v.ItemNo != itemNo &&
            v.TarifId == tarifId &&
            v.KelasId == kelasId &&
            v.TipeTarifId == tipeTarifId);

    private bool HasVariant(string tarifId, string kelasId, string tipeTarifId) =>
        _variants.Any(v =>
            v.TarifId == tarifId &&
            v.KelasId == kelasId &&
            v.TipeTarifId == tipeTarifId);

    private int NextItemNo() =>
        _variants.Count == 0 ? 1 : _variants.Max(v => v.ItemNo) + 1;

    private TarifPolicyType CloneWithVariants(IReadOnlyList<TarifVariantType> variants) =>
        new(
            TarifPolicyId,
            PolicyNo,
            PolicyName,
            EffectiveDateInfo,
            Description,
            PolicyStatus,
            AuditTrail,
            variants);

    #endregion
}

public interface ITarifPolicyKey
{
    string TarifPolicyId { get; }
}
