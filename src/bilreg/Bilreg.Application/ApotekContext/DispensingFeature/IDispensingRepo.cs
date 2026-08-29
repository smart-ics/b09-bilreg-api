using Bilreg.Domain.ApotekContext.DispensingFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.DispensingFeature;

public interface IDispensingRepo :
    ISaveChange<DispensingModel>,
    ILoadEntity<DispensingModel, IDispensingKey>
{
    IReadOnlyList<DispensingModel> ListBySalesOrder(string salesOrderId);
}
