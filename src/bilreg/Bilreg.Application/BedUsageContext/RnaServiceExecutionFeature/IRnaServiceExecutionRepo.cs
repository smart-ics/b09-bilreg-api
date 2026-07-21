using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.RnaServiceExecutionFeature;

public interface IRnaServiceExecutionRepo :
    ISaveChange<RnaServiceExecutionModel>,
    ILoadEntity<RnaServiceExecutionModel, IRnaServiceExecutionKey>,
    IDeleteEntity<IRnaServiceExecutionKey>
{
}
