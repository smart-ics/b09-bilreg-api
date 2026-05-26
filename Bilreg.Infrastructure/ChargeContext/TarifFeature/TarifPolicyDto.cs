using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record TarifPolicyDto(
    string TarifPolicyId,
    string PolicyNo,
    string PolicyName,
    DateTime EffectiveDateInfo,
    string Description,
    int PolicyStatus,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static TarifPolicyDto FromModel(TarifPolicyType model) =>
        new(
            model.TarifPolicyId,
            model.PolicyNo,
            model.PolicyName,
            model.EffectiveDateInfo,
            model.Description,
            (int)model.PolicyStatus,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);

    public TarifPolicyType ToModel(IEnumerable<TarifVariantType> variants)
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        return new TarifPolicyType(
            TarifPolicyId,
            PolicyNo,
            PolicyName,
            EffectiveDateInfo,
            Description,
            (TarifPolicyStatus)PolicyStatus,
            audit,
            variants);
    }
}
