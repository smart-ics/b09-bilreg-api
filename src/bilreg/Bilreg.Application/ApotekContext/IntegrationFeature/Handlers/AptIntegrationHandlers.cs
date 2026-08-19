using System.Text.Json;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;

namespace Bilreg.Application.ApotekContext.IntegrationFeature.Handlers;

public class TrackerServedAtHandler : IAptIntegrationHandler
{
    private readonly ITrackerPharmacyPort _tracker;
    public TrackerServedAtHandler(ITrackerPharmacyPort tracker) => _tracker = tracker;
    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.TrackerServedAt;

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        var p = JsonSerializer.Deserialize<TrackerPayload>(task.PayloadJson)!;
        var result = _tracker.ServeOnce(p.AntrianId, p.NoUrut, p.PasienTrackerId, p.ReffId, DateTime.Now);
        return new AptIntegrationHandleResult(result.Applied || result.CorrelationId.Length > 0, result.CorrelationId, null);
    }
}

public class TrackerDoneAtPickupHandler : IAptIntegrationHandler
{
    private readonly ITrackerPharmacyPort _tracker;
    public TrackerDoneAtPickupHandler(ITrackerPharmacyPort tracker) => _tracker = tracker;
    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.TrackerDoneAtPickup;

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        var p = JsonSerializer.Deserialize<TrackerPayload>(task.PayloadJson)!;
        var result = _tracker.DoneOnce(p.AntrianId, p.NoUrut, p.PasienTrackerId, p.ReffId ?? task.IdempotencyKey, DateTime.Now);
        return new AptIntegrationHandleResult(true, result.CorrelationId, null);
    }
}

public class TrackerDoneAtNoShowHandler : IAptIntegrationHandler
{
    private readonly ITrackerPharmacyPort _tracker;
    public TrackerDoneAtNoShowHandler(ITrackerPharmacyPort tracker) => _tracker = tracker;
    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.TrackerDoneAtNoShow;

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        var p = JsonSerializer.Deserialize<TrackerPayload>(task.PayloadJson)!;
        var result = _tracker.DoneOnce(p.AntrianId, p.NoUrut, p.PasienTrackerId, p.ReffId ?? task.IdempotencyKey, DateTime.Now);
        return new AptIntegrationHandleResult(true, result.CorrelationId, null);
    }
}

public class TrackerWithdrawnHandler : IAptIntegrationHandler
{
    private readonly ITrackerPharmacyPort _tracker;
    public TrackerWithdrawnHandler(ITrackerPharmacyPort tracker) => _tracker = tracker;
    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.TrackerWithdrawn;

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        var p = JsonSerializer.Deserialize<WithdrawPayload>(task.PayloadJson)!;
        var result = _tracker.WithdrawFromWaiting(p.AntrianId, p.NoUrut, p.Reason, p.UserId, DateTime.Now);
        return new AptIntegrationHandleResult(result.Applied, result.CorrelationId, result.Applied ? null : "withdraw failed");
    }
}

public class StockReserveHandler : IAptIntegrationHandler
{
    private readonly IStockPharmacyPort _stock;
    public StockReserveHandler(IStockPharmacyPort stock) => _stock = stock;
    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.StockReserve;

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        var p = JsonSerializer.Deserialize<StockPayload>(task.PayloadJson)!;
        var reff = _stock.ReserveToTemporaryUnit(p.DispensingId, p.ItemNo, p.BrgId, p.Qty);
        return new AptIntegrationHandleResult(true, reff, null);
    }
}

public class StockRemoveOnHandoverHandler : IAptIntegrationHandler
{
    private readonly IStockPharmacyPort _stock;
    public StockRemoveOnHandoverHandler(IStockPharmacyPort stock) => _stock = stock;
    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.StockRemoveOnHandover;

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        var p = JsonSerializer.Deserialize<StockPayload>(task.PayloadJson)!;
        var reff = _stock.RemoveOnHandover(p.DispensingId, p.ItemNo, p.BrgId, p.Qty);
        return new AptIntegrationHandleResult(true, reff, null);
    }
}

public class StockReturnNoShowHandler : IAptIntegrationHandler
{
    private readonly IStockPharmacyPort _stock;
    public StockReturnNoShowHandler(IStockPharmacyPort stock) => _stock = stock;
    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.StockReturnNoShow;

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        var p = JsonSerializer.Deserialize<StockPayload>(task.PayloadJson)!;
        var reff = _stock.ReturnOnNoShow(p.DispensingId, p.ItemNo, p.BrgId, p.Qty);
        return new AptIntegrationHandleResult(true, reff, null);
    }
}

public class BillingChargeHandler : IAptIntegrationHandler
{
    private readonly ITataRekeningChargePort _charge;
    private readonly IInvoiceRepo _invoiceRepo;
    public BillingChargeHandler(ITataRekeningChargePort charge, IInvoiceRepo invoiceRepo)
    {
        _charge = charge;
        _invoiceRepo = invoiceRepo;
    }

    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.BillingCharge;

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        var invoice = _invoiceRepo.LoadEntity(InvoiceModel.Key(task.SourceId));
        if (!invoice.HasValue)
            return new AptIntegrationHandleResult(false, "", "invoice not found");
        if (!string.IsNullOrWhiteSpace(invoice.Value.TataRekeningChargeId))
            return new AptIntegrationHandleResult(true, invoice.Value.TataRekeningChargeId, null);
        var reff = _charge.IssueCharge(invoice.Value);
        invoice.Value.RecordChargeCorrelation(reff);
        _invoiceRepo.SaveChanges(invoice.Value);
        return new AptIntegrationHandleResult(true, reff, null);
    }
}

public class IterConsumeHandler : IAptIntegrationHandler
{
    private readonly IIterConsumePort _iter;
    public IterConsumeHandler(IIterConsumePort iter) => _iter = iter;
    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.IterConsume;

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        var p = JsonSerializer.Deserialize<IterPayload>(task.PayloadJson)!;
        var reff = _iter.Consume(p.SourceResepId, p.ConsumeCount);
        return new AptIntegrationHandleResult(true, reff, null);
    }
}

internal record TrackerPayload(string AntrianId, int NoUrut, string PasienTrackerId, string? ReffId);
internal record WithdrawPayload(string AntrianId, int NoUrut, string Reason, string UserId);
internal record StockPayload(string DispensingId, int ItemNo, string BrgId, decimal Qty);
internal record IterPayload(string SourceResepId, int ConsumeCount);
