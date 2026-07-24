using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;

public interface IEmrAntrianOutboundQueueRepo :
    ISaveChange<EmrAntrianOutboundQueueModel>,
    ILoadEntity<EmrAntrianOutboundQueueModel, IEmrAntrianOutboundQueueKey>
{
    IEnumerable<EmrAntrianOutboundQueueModel> ListProcessable(int batchSize);

    MayBe<EmrAntrianOutboundQueueModel> FindActiveBySource(string sourceId, string messageType);
}
