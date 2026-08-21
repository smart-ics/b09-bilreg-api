using Bilreg.Domain.ApotekContext.QueueFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature;

public interface IQueueMappingRepo :
    ISaveChange<QueueMappingModel>,
    ILoadEntity<QueueMappingModel, IQueueMappingKey>
{
    IReadOnlyList<QueueMappingModel> ListByQueue(string antrianId, int noUrut);
}

public interface IQueueCloseRepo :
    ISaveChange<QueueCloseModel>,
    ILoadEntity<QueueCloseModel, IQueueCloseKey>
{
    MayBe<QueueCloseModel> LoadByQueue(string antrianId, int noUrut);
}
