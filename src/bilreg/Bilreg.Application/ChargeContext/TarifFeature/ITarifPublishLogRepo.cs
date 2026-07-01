using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface ITarifPublishLogRepo :
    ILoadEntity<TarifPublishLogType, ITarifPublishLogKey>
{
    void Insert(TarifPublishLogType log);
    IEnumerable<TarifPublishLogType> ListByPolicy(ITarifPolicyKey policyKey);
}
