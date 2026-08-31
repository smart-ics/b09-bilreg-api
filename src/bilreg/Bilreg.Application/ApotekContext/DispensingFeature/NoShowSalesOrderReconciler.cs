using Bilreg.Application.ApotekContext.DispensingFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;

namespace Bilreg.Application.ApotekContext.DispensingFeature;

internal static class NoShowSalesOrderReconciler
{
    internal static void TryResolveAfterStockReturn(
        DispensingModel dispensing,
        IDispensingRepo dispensingRepo,
        ISalesOrderRepo salesOrderRepo,
        IInvoiceRepo invoiceRepo)
    {
        if (dispensing.DispensingStatus != DispensingStatusEnum.Expired)
            return;

        foreach (var sibling in dispensingRepo.ListBySalesOrder(dispensing.SalesOrderId))
        {
            if (sibling.DispensingStatus != DispensingStatusEnum.Expired)
                continue;
            if (sibling.Items.Any(x => string.IsNullOrWhiteSpace(x.ReturnMutasiReff)))
                return;
        }

        var order = salesOrderRepo.LoadEntity(SalesOrderModel.Key(dispensing.SalesOrderId));
        if (!order.HasValue || !order.Value.IsActiveKey)
            return;

        if (order.Value.Items.Any(x => x.UnresolvedAcceptedQty > 0))
            return;

        if (RequiresCommercialResolution(order.Value, invoiceRepo))
            return;

        order.Value.Resolve(SalesOrderResolvedReasonEnum.CollectionWindowExpired);
        salesOrderRepo.SaveChanges(order.Value);
    }

    private static bool RequiresCommercialResolution(SalesOrderModel order, IInvoiceRepo invoiceRepo)
    {
        if (order.PayerPath != PayerPathEnum.GeneralPatientPay)
            return false;

        var invoice = invoiceRepo.LoadActiveBySalesOrder(order.SalesOrderId);
        return invoice.HasValue && invoice.Value.HasPaymentClearance;
    }
}
