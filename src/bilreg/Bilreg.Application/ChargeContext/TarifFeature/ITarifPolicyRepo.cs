using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface ITarifPolicyRepo :
    ISaveChange<TarifPolicyType>,
    ILoadEntity<TarifPolicyType, ITarifPolicyKey>
{
    IEnumerable<TarifPolicySummaryView> ListData(TarifPolicyListFilter filter);
}

public record TarifPolicyListFilter(
    TarifPolicyStatus? PolicyStatus = null,
    string Keyword = "");

public record TarifPolicySummaryView(
    string TarifPolicyId,
    string PolicyNo,
    string PolicyName,
    DateTime EffectiveDateInfo,
    TarifPolicyStatus PolicyStatus,
    int VariantCount);
