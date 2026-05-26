using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record TarifPolicyType : ITarifPolicyKey
{
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

    public static TarifPolicyType Default => new(
        "-", "", "", new DateTime(3000, 1, 1), "", TarifPolicyStatus.Draft,
        AuditTrailType.Default, []);

    public static ITarifPolicyKey Key(string id) => Default with { TarifPolicyId = id };

    public string TarifPolicyId { get; init; }
    public string PolicyNo { get; init; }
    public string PolicyName { get; init; }
    public DateTime EffectiveDateInfo { get; init; }
    public string Description { get; init; }
    public TarifPolicyStatus PolicyStatus { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public IEnumerable<TarifVariantType> Variants => _variants;
}

public interface ITarifPolicyKey
{
    string TarifPolicyId { get; }
}
