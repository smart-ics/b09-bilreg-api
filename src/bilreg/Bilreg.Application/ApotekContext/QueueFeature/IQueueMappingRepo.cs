using Bilreg.Domain.ApotekContext.QueueFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature;

public interface IQueueMappingRepo : IPharmacyQueueMappingRepo
{
}

public interface IQueueCloseRepo :
    ISaveChange<QueueCloseModel>,
    ILoadEntity<QueueCloseModel, IQueueCloseKey>
{
    MayBe<QueueCloseModel> LoadByQueue(string antrianId, int noUrut);
}
