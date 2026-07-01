using Bilreg.Domain.LabContext.LabOwareFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOwareFeature;

public interface ILabOwareOutboundQueueRepo :
    ISaveChange<LabOwareOutboundQueueModel>,
    ILoadEntity<LabOwareOutboundQueueModel, ILabOwareOutboundQueueKey>
{
    IEnumerable<LabOwareOutboundQueueModel> ListProcessable(int batchSize);
}
