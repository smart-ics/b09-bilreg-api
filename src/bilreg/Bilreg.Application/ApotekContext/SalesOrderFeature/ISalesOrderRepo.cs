using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.SalesOrderFeature;

public interface ISalesOrderRepo :
    ISaveChange<SalesOrderModel>,
    ILoadEntity<SalesOrderModel, ISalesOrderKey>
{
    MayBe<SalesOrderModel> LoadActive(SalesOrderSourceKindEnum sourceKind, string sourceId, string regId, PayerPathEnum payerPath);
    IReadOnlyList<SalesOrderModel> ListBySource(SalesOrderSourceKindEnum sourceKind, string sourceId);
}
