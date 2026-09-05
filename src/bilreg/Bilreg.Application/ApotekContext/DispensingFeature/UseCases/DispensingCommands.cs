using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.DispensingFeature.UseCases;

public record DispensingEstablishCmd(string UserId, string SalesOrderId, List<DispensingEstablishItem> Items)
    : IRequest<DispensingResponse>;

public record DispensingEstablishItem(int SalesOrderItemNo, decimal Qty);

public record DispensingReleaseCmd(string UserId, string DispensingId, int ExpectedVersion) : IRequest<DispensingResponse>;
public record DispensingStartCmd(string UserId, string DispensingId, int ExpectedVersion, string AntrianId, int NoUrut, string PasienTrackerId)
    : IRequest<DispensingResponse>;
public record DispensingPrepareCmd(string UserId, string DispensingId, int ExpectedVersion) : IRequest<DispensingResponse>;
public record DispensingPickupCallCmd(string UserId, string AntrianId, int NoUrut, string PasienTrackerId)
    : IRequest<DispensingPickupResponse>;
public record DispensingFinalReviewCmd(string UserId, string DispensingId, int ExpectedVersion, FinalReviewOutcomeEnum Outcome, string Reason)
    : IRequest<DispensingResponse>;
public record DispensingEducationCmd(string UserId, string DispensingId, int ExpectedVersion, string Note)
    : IRequest<DispensingResponse>;
public record DispensingOverrideCmd(string UserId, string DispensingId, int ExpectedVersion, string Reason)
    : IRequest<DispensingResponse>;
public record DispensingHandoverCmd(
    string UserId, string DispensingId, int ExpectedVersion, string RecipientPhone, string RecipientRelationship)
    : IRequest<DispensingResponse>;
public record DispensingNoShowCmd(string UserId, string DispensingId, int ExpectedVersion, string AntrianId, int NoUrut, string PasienTrackerId, string Reason)
    : IRequest<DispensingResponse>;

public record DispensingResponse(string DispensingId, DispensingStatusEnum Status, int Version);
public record DispensingPickupResponse(int PreparedCount, int ResolvedCount);

public class DispensingEstablishHandler : IRequestHandler<DispensingEstablishCmd, DispensingResponse>
{
    private readonly IDispensingRepo _repo;
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingEstablishHandler(IDispensingRepo repo, ISalesOrderRepo salesOrderRepo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _salesOrderRepo = salesOrderRepo;
        _auth = auth;
    }

    public Task<DispensingResponse> Handle(DispensingEstablishCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingEstablishCmd), request.UserId);
        var so = _salesOrderRepo.LoadEntity(SalesOrderModel.Key(request.SalesOrderId))
            .GetValueOrThrow($"Sales Order '{request.SalesOrderId}' not found");
        var items = request.Items.Select((x, i) =>
        {
            var soItem = so.Item(x.SalesOrderItemNo);
            if (x.Qty > soItem.UnresolvedAcceptedQty)
                throw new ApotekDomainException("Dispensing qty exceeds unresolved Accepted Qty.");
            return new DispensingItemModel(i + 1, x.SalesOrderItemNo, soItem.BrgId, x.Qty, "", "", "", DispensingItemOutcomeEnum.Open);
        }).ToList();
        var model = DispensingModel.Establish(
            so.SalesOrderId, ApotekLocationIds.PharmacyUnitLayananId, ApotekLocationIds.DispensingTemporaryUnitLayananId, items);
        if (so.PayerPath == PayerPathEnum.GeneralPatientPay)
            model.AwaitClearance();
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(model);
        trans.Complete();
        return Task.FromResult(new DispensingResponse(model.DispensingId, model.DispensingStatus, model.Version));
    }
}

public class DispensingReleaseHandler : IRequestHandler<DispensingReleaseCmd, DispensingResponse>
{
    private readonly IDispensingRepo _repo;
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IInvoiceRepo _invoiceRepo;
    private readonly DispenseAuthorizedPolicy _policy;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingReleaseHandler(
        IDispensingRepo repo, ISalesOrderRepo salesOrderRepo, IInvoiceRepo invoiceRepo,
        DispenseAuthorizedPolicy policy, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _salesOrderRepo = salesOrderRepo;
        _invoiceRepo = invoiceRepo;
        _policy = policy;
        _auth = auth;
    }

    public Task<DispensingResponse> Handle(DispensingReleaseCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingReleaseCmd), request.UserId);
        var d = _repo.LoadEntity(DispensingModel.Key(request.DispensingId))
            .GetValueOrThrow($"Dispensing '{request.DispensingId}' not found");
        d.AssertExpectedVersion(request.ExpectedVersion);
        var so = _salesOrderRepo.LoadEntity(SalesOrderModel.Key(d.SalesOrderId)).GetValueOrThrow("Sales Order not found");
        var invoice = _invoiceRepo.LoadActiveBySalesOrder(so.SalesOrderId);
        d.Release(DateTime.Now, _policy.IsAuthorized(so, invoice.HasValue ? invoice.Value : null));
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(d);
        trans.Complete();
        return Task.FromResult(new DispensingResponse(d.DispensingId, d.DispensingStatus, d.Version));
    }
}

public class DispensingStartHandler : IRequestHandler<DispensingStartCmd, DispensingResponse>
{
    private readonly IDispensingRepo _repo;
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IInvoiceRepo _invoiceRepo;
    private readonly IAptIntegrationTaskRepo _taskRepo;
    private readonly DispenseAuthorizedPolicy _policy;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingStartHandler(
        IDispensingRepo repo, ISalesOrderRepo salesOrderRepo, IInvoiceRepo invoiceRepo,
        IAptIntegrationTaskRepo taskRepo, DispenseAuthorizedPolicy policy, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _salesOrderRepo = salesOrderRepo;
        _invoiceRepo = invoiceRepo;
        _taskRepo = taskRepo;
        _policy = policy;
        _auth = auth;
    }

    public Task<DispensingResponse> Handle(DispensingStartCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingStartCmd), request.UserId);
        var d = _repo.LoadEntity(DispensingModel.Key(request.DispensingId))
            .GetValueOrThrow($"Dispensing '{request.DispensingId}' not found");
        d.AssertExpectedVersion(request.ExpectedVersion);
        var so = _salesOrderRepo.LoadEntity(SalesOrderModel.Key(d.SalesOrderId)).GetValueOrThrow("Sales Order not found");
        var invoice = _invoiceRepo.LoadActiveBySalesOrder(so.SalesOrderId);
        var first = d.StartPreparation(DateTime.Now, _policy.IsAuthorized(so, invoice.HasValue ? invoice.Value : null));
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(d);
        if (first)
        {
            foreach (var item in d.Items)
            {
                AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
                    AptIntegrationTaskTypeEnum.StockReserve,
                    AptIntegrationSourceKindEnum.Dispensing,
                    d.DispensingId,
                    $"{d.DispensingId}:I{item.ItemNo}:RESERVE",
                    AptIntegrationDestinationEnum.StockLedger,
                    System.Text.Json.JsonSerializer.Serialize(new { d.DispensingId, item.ItemNo, item.BrgId, item.Qty })));
            }
            AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
                AptIntegrationTaskTypeEnum.TrackerServedAt,
                AptIntegrationSourceKindEnum.Dispensing,
                d.DispensingId,
                $"{request.AntrianId}:{request.NoUrut}:SERVE",
                AptIntegrationDestinationEnum.Tracker,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.AntrianId,
                    request.NoUrut,
                    request.PasienTrackerId,
                    ReffId = d.DispensingId
                })));
        }
        trans.Complete();
        return Task.FromResult(new DispensingResponse(d.DispensingId, d.DispensingStatus, d.Version));
    }
}

public class DispensingPrepareHandler : IRequestHandler<DispensingPrepareCmd, DispensingResponse>
{
    private readonly IDispensingRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingPrepareHandler(IDispensingRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task<DispensingResponse> Handle(DispensingPrepareCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingPrepareCmd), request.UserId);
        var d = _repo.LoadEntity(DispensingModel.Key(request.DispensingId)).GetValueOrThrow("Dispensing not found");
        d.AssertExpectedVersion(request.ExpectedVersion);
        d.MarkPrepared(DateTime.Now);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(d);
        trans.Complete();
        return Task.FromResult(new DispensingResponse(d.DispensingId, d.DispensingStatus, d.Version));
    }
}

public class DispensingPickupCallHandler : IRequestHandler<DispensingPickupCallCmd, DispensingPickupResponse>
{
    private readonly IQueueMappingRepo _mappingRepo;
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IDispensingRepo _dispensingRepo;
    private readonly IAptIntegrationTaskRepo _taskRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingPickupCallHandler(
        IQueueMappingRepo mappingRepo,
        ISalesOrderRepo salesOrderRepo,
        IDispensingRepo dispensingRepo,
        IAptIntegrationTaskRepo taskRepo,
        IAptAuthorizationPolicy auth)
    {
        _mappingRepo = mappingRepo;
        _salesOrderRepo = salesOrderRepo;
        _dispensingRepo = dispensingRepo;
        _taskRepo = taskRepo;
        _auth = auth;
    }

    public Task<DispensingPickupResponse> Handle(DispensingPickupCallCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingPickupCallCmd), request.UserId);
        var mappings = _mappingRepo.ListByQueue(request.AntrianId, request.NoUrut);
        if (mappings.Count == 0)
            throw new ApotekDomainException("Pickup call requires mapped medication demands.");

        var preparedForPickup = new List<DispensingModel>();
        var resolvedCount = 0;
        foreach (var map in mappings)
        {
            var sourceKind = map.DemandKind == Domain.ApotekContext.QueueFeature.QueueDemandKindEnum.ResepKerja
                ? SalesOrderSourceKindEnum.ResepKerja
                : SalesOrderSourceKindEnum.JualBebas;
            var orders = _salesOrderRepo.ListBySource(sourceKind, map.DemandId);
            if (orders.Count == 0)
                throw new ApotekDomainException("Pickup call requires every mapped demand Established or accountably resolved.");

            var activeOrders = orders.Where(x => x.IsActiveKey).ToList();
            var demandDispensings = orders
                .SelectMany(so => _dispensingRepo.ListBySalesOrder(so.SalesOrderId))
                .ToList();
            resolvedCount += demandDispensings.Count(x => x.IsAccountablyResolved);

            if (activeOrders.Count == 0)
                continue;

            var activeDispensings = activeOrders
                .SelectMany(so => _dispensingRepo.ListBySalesOrder(so.SalesOrderId))
                .ToList();
            if (activeDispensings.Count == 0
                || activeDispensings.Any(x => !x.IsPrepared && !x.IsAccountablyResolved))
                throw new ApotekDomainException("Pickup call requires every intended Dispensing Prepared or accountably resolved.");

            preparedForPickup.AddRange(activeDispensings.Where(x => x.IsPrepared));
        }

        if (preparedForPickup.Count == 0)
            throw new ApotekDomainException("Pickup call requires every intended Dispensing Prepared or accountably resolved.");

        using var trans = TransHelper.NewScope();
        foreach (var d in preparedForPickup)
        {
            d.RecordPickupCall(DateTime.Now);
            _dispensingRepo.SaveChanges(d);
        }
        AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerDoneAtPickup,
            AptIntegrationSourceKindEnum.Mapping,
            $"{request.AntrianId}:{request.NoUrut}",
            $"{request.AntrianId}:{request.NoUrut}:DONE",
            AptIntegrationDestinationEnum.Tracker,
            System.Text.Json.JsonSerializer.Serialize(new { request.AntrianId, request.NoUrut, request.PasienTrackerId })));
        trans.Complete();
        return Task.FromResult(new DispensingPickupResponse(preparedForPickup.Count, resolvedCount));
    }
}

public class DispensingFinalReviewHandler : IRequestHandler<DispensingFinalReviewCmd, DispensingResponse>
{
    private readonly IDispensingRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingFinalReviewHandler(IDispensingRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task<DispensingResponse> Handle(DispensingFinalReviewCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingFinalReviewCmd), request.UserId);
        var d = _repo.LoadEntity(DispensingModel.Key(request.DispensingId)).GetValueOrThrow("Dispensing not found");
        d.AssertExpectedVersion(request.ExpectedVersion);
        d.AppendFinalReview(request.Outcome, request.Reason, request.UserId, DateTime.Now);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(d);
        trans.Complete();
        return Task.FromResult(new DispensingResponse(d.DispensingId, d.DispensingStatus, d.Version));
    }
}

public class DispensingEducationHandler : IRequestHandler<DispensingEducationCmd, DispensingResponse>
{
    private readonly IDispensingRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingEducationHandler(IDispensingRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task<DispensingResponse> Handle(DispensingEducationCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingEducationCmd), request.UserId);
        var d = _repo.LoadEntity(DispensingModel.Key(request.DispensingId)).GetValueOrThrow("Dispensing not found");
        d.AssertExpectedVersion(request.ExpectedVersion);
        d.RecordEducation(request.UserId, DateTime.Now, request.Note);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(d);
        trans.Complete();
        return Task.FromResult(new DispensingResponse(d.DispensingId, d.DispensingStatus, d.Version));
    }
}

public class DispensingOverrideHandler : IRequestHandler<DispensingOverrideCmd, DispensingResponse>
{
    private readonly IDispensingRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingOverrideHandler(IDispensingRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task<DispensingResponse> Handle(DispensingOverrideCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingOverrideCmd), request.UserId);
        var d = _repo.LoadEntity(DispensingModel.Key(request.DispensingId)).GetValueOrThrow("Dispensing not found");
        d.AssertExpectedVersion(request.ExpectedVersion);
        d.OverrideCollectionWindow(request.UserId, request.Reason, DateTime.Now);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(d);
        trans.Complete();
        return Task.FromResult(new DispensingResponse(d.DispensingId, d.DispensingStatus, d.Version));
    }
}

public class DispensingHandoverHandler : IRequestHandler<DispensingHandoverCmd, DispensingResponse>
{
    private readonly IDispensingRepo _repo;
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IInvoiceRepo _invoiceRepo;
    private readonly IMedicationPricePort _price;
    private readonly IAptIntegrationTaskRepo _taskRepo;
    private readonly ICollectionWindowDaysProvider _window;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingHandoverHandler(
        IDispensingRepo repo,
        ISalesOrderRepo salesOrderRepo,
        IInvoiceRepo invoiceRepo,
        IMedicationPricePort price,
        IAptIntegrationTaskRepo taskRepo,
        ICollectionWindowDaysProvider window,
        IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _salesOrderRepo = salesOrderRepo;
        _invoiceRepo = invoiceRepo;
        _price = price;
        _taskRepo = taskRepo;
        _window = window;
        _auth = auth;
    }

    public Task<DispensingResponse> Handle(DispensingHandoverCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingHandoverCmd), request.UserId);
        var d = _repo.LoadEntity(DispensingModel.Key(request.DispensingId)).GetValueOrThrow("Dispensing not found");
        d.AssertExpectedVersion(request.ExpectedVersion);
        var so = _salesOrderRepo.LoadEntity(SalesOrderModel.Key(d.SalesOrderId)).GetValueOrThrow("Sales Order not found");
        var expired = d.IsPickupExpired(DateTime.Now, _window.GetDays());
        var hasOverride = !ApotekDate.IsEmpty(d.OverrideAt);
        d.Handover(DateTime.Now, request.RecipientPhone, request.RecipientRelationship, expired, hasOverride);
        foreach (var item in d.Items)
            so.ApplyDispenseQty(item.SalesOrderItemNo, item.Qty);

        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(d);
        _salesOrderRepo.SaveChanges(so);
        foreach (var item in d.Items)
        {
            AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
                AptIntegrationTaskTypeEnum.StockRemoveOnHandover,
                AptIntegrationSourceKindEnum.Dispensing,
                d.DispensingId,
                $"{d.DispensingId}:I{item.ItemNo}:ISSUE",
                AptIntegrationDestinationEnum.StockLedger,
                System.Text.Json.JsonSerializer.Serialize(new { d.DispensingId, item.ItemNo, item.BrgId, item.Qty })));
        }

        if (so.PayerPath == PayerPathEnum.Bpjs && !_invoiceRepo.LoadActiveBySalesOrder(so.SalesOrderId).HasValue)
        {
            var snapshotAt = DateTime.Now;
            var invoiceItems = d.Items.Select((x, i) =>
            {
                var soItem = so.Item(x.SalesOrderItemNo);
                var price = _price.PriceAt(x.BrgId, snapshotAt);
                return new Domain.ApotekContext.InvoiceFeature.InvoiceItemModel(
                    i + 1, x.SalesOrderItemNo, x.BrgId, soItem.BrgName, Domain.ApotekContext.InvoiceFeature.InvoiceItemKindEnum.Medication,
                    x.Qty, price.HargaSatuan, 0, 0, 0, price.HargaSatuan * x.Qty);
            }).ToList();
            var invoice = Domain.ApotekContext.InvoiceFeature.InvoiceModel.Establish(
                so.SalesOrderId, so.PayerPath, snapshotAt, "BPJS", "BPJS", snapshotAt, invoiceItems, [], 0, 0, 0);
            invoice.Issue(snapshotAt);
            _invoiceRepo.SaveChanges(invoice);
            AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
                AptIntegrationTaskTypeEnum.BillingCharge,
                AptIntegrationSourceKindEnum.Invoice,
                invoice.InvoiceId,
                $"{invoice.InvoiceId}:BILL",
                AptIntegrationDestinationEnum.TataRekening,
                System.Text.Json.JsonSerializer.Serialize(new { invoice.InvoiceId, invoice.GrandTotal })));
        }
        trans.Complete();
        return Task.FromResult(new DispensingResponse(d.DispensingId, d.DispensingStatus, d.Version));
    }
}

public class DispensingNoShowHandler : IRequestHandler<DispensingNoShowCmd, DispensingResponse>
{
    private readonly IDispensingRepo _repo;
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IAptIntegrationTaskRepo _taskRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public DispensingNoShowHandler(
        IDispensingRepo repo, ISalesOrderRepo salesOrderRepo, IAptIntegrationTaskRepo taskRepo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _salesOrderRepo = salesOrderRepo;
        _taskRepo = taskRepo;
        _auth = auth;
    }

    public Task<DispensingResponse> Handle(DispensingNoShowCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(DispensingNoShowCmd), request.UserId);
        var d = _repo.LoadEntity(DispensingModel.Key(request.DispensingId)).GetValueOrThrow("Dispensing not found");
        d.AssertExpectedVersion(request.ExpectedVersion);
        d.ExpireNoShow(DateTime.Now, request.Reason);
        var so = _salesOrderRepo.LoadEntity(SalesOrderModel.Key(d.SalesOrderId)).GetValueOrThrow("Sales Order not found");
        foreach (var item in d.Items)
            so.AppendUnfulfilled(item.SalesOrderItemNo, item.Qty, UnfulfilledReasonEnum.CollectionWindowExpired, "", request.UserId, DateTime.Now);

        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(d);
        _salesOrderRepo.SaveChanges(so);
        foreach (var item in d.Items)
        {
            AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
                AptIntegrationTaskTypeEnum.StockReturnNoShow,
                AptIntegrationSourceKindEnum.Dispensing,
                d.DispensingId,
                $"{d.DispensingId}:I{item.ItemNo}:RETURN",
                AptIntegrationDestinationEnum.StockLedger,
                System.Text.Json.JsonSerializer.Serialize(new { d.DispensingId, item.ItemNo, item.BrgId, item.Qty })));
        }
        AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerDoneAtNoShow,
            AptIntegrationSourceKindEnum.Dispensing,
            d.DispensingId,
            $"{request.AntrianId}:{request.NoUrut}:DONE",
            AptIntegrationDestinationEnum.Tracker,
            System.Text.Json.JsonSerializer.Serialize(new { request.AntrianId, request.NoUrut, request.PasienTrackerId })));
        trans.Complete();
        return Task.FromResult(new DispensingResponse(d.DispensingId, d.DispensingStatus, d.Version));
    }
}
