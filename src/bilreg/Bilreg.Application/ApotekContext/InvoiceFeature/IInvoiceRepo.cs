using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.InvoiceFeature;

public interface IInvoiceRepo :
    ISaveChange<InvoiceModel>,
    ILoadEntity<InvoiceModel, IInvoiceKey>
{
    IReadOnlyList<InvoiceModel> ListBySalesOrder(string salesOrderId);
    MayBe<InvoiceModel> LoadActiveBySalesOrder(string salesOrderId);
}
