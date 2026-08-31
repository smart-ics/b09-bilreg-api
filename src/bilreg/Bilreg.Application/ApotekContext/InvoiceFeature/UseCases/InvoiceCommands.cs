using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;

public record InvoiceEstablishCmd(
    string UserId,
    string SalesOrderId,
    string TipeJaminanId,
    string TipeJaminanName,
    decimal DiskonLain,
    decimal BiayaLain,
    decimal Pembulatan)
    : IRequest<InvoiceResponse>;

public record InvoiceIssueCmd(string UserId, string InvoiceId, int ExpectedVersion) : IRequest<InvoiceResponse>;

public record InvoiceRecordPaymentCmd(
    string UserId,
    string InvoiceId,
    int ExpectedVersion,
    string PaymentClearanceReff,
    DateTime PaymentClearedAt)
    : IRequest<InvoiceResponse>;

public record InvoiceReviseCmd(
    string UserId,
    string InvoiceId,
    int ExpectedVersion,
    List<InvoiceItemModel> Items,
    List<InvoiceItemChargeModel> Charges,
    decimal DiskonLain,
    decimal BiayaLain,
    decimal Pembulatan)
    : IRequest<InvoiceResponse>;

public record InvoiceRecordCorrectionCmd(string UserId, string InvoiceId, int ExpectedVersion, string CorrectionReff)
    : IRequest<InvoiceResponse>;

public record InvoiceResponse(string InvoiceId, InvoiceStatusEnum Status, int Version);

public class InvoiceEstablishHandler : IRequestHandler<InvoiceEstablishCmd, InvoiceResponse>
{
    private readonly IInvoiceRepo _invoiceRepo;
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IMedicationPricePort _price;
    private readonly IAptAuthorizationPolicy _auth;

    public InvoiceEstablishHandler(
        IInvoiceRepo invoiceRepo,
        ISalesOrderRepo salesOrderRepo,
        IMedicationPricePort price,
        IAptAuthorizationPolicy auth)
    {
        _invoiceRepo = invoiceRepo;
        _salesOrderRepo = salesOrderRepo;
        _price = price;
        _auth = auth;
    }

    public Task<InvoiceResponse> Handle(InvoiceEstablishCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(InvoiceEstablishCmd), request.UserId);
        var so = _salesOrderRepo.LoadEntity(SalesOrderModel.Key(request.SalesOrderId))
            .GetValueOrThrow($"Sales Order '{request.SalesOrderId}' not found");
        if (so.PayerPath == PayerPathEnum.Bpjs)
            throw new ApotekDomainException("BPJS Invoice is established at handover, not before.");
        var snapshotAt = DateTime.Now;
        var items = so.Items.Select((x, i) =>
        {
            var price = _price.PriceAt(x.BrgId, snapshotAt);
            var total = price.HargaSatuan * x.AcceptedQty;
            return new InvoiceItemModel(i + 1, x.ItemNo, x.BrgId, x.BrgName, InvoiceItemKindEnum.Medication,
                x.AcceptedQty, price.HargaSatuan, 0, 0, price.Tax * x.AcceptedQty, total);
        }).ToList();
        var invoice = InvoiceModel.Establish(
            so.SalesOrderId, so.PayerPath, snapshotAt, request.TipeJaminanId, request.TipeJaminanName,
            snapshotAt, items, [], request.DiskonLain, request.BiayaLain, request.Pembulatan);
        using var trans = TransHelper.NewScope();
        _invoiceRepo.SaveChanges(invoice);
        foreach (var item in items)
            so.ApplyInvoiceQty(item.SalesOrderItemNo, item.Qty);
        _salesOrderRepo.SaveChanges(so);
        trans.Complete();
        return Task.FromResult(new InvoiceResponse(invoice.InvoiceId, invoice.InvoiceStatus, invoice.Version));
    }
}

public class InvoiceIssueHandler : IRequestHandler<InvoiceIssueCmd, InvoiceResponse>
{
    private readonly IInvoiceRepo _repo;
    private readonly IAptIntegrationTaskRepo _taskRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public InvoiceIssueHandler(IInvoiceRepo repo, IAptIntegrationTaskRepo taskRepo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _taskRepo = taskRepo;
        _auth = auth;
    }

    public Task<InvoiceResponse> Handle(InvoiceIssueCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(InvoiceIssueCmd), request.UserId);
        var invoice = _repo.LoadEntity(InvoiceModel.Key(request.InvoiceId))
            .GetValueOrThrow($"Invoice '{request.InvoiceId}' not found");
        invoice.AssertExpectedVersion(request.ExpectedVersion);
        invoice.Issue(DateTime.Now);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(invoice);
        AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.BillingCharge,
            AptIntegrationSourceKindEnum.Invoice,
            invoice.InvoiceId,
            $"{invoice.InvoiceId}:BILL",
            AptIntegrationDestinationEnum.TataRekening,
            System.Text.Json.JsonSerializer.Serialize(new { invoice.InvoiceId, invoice.GrandTotal })));
        trans.Complete();
        return Task.FromResult(new InvoiceResponse(invoice.InvoiceId, invoice.InvoiceStatus, invoice.Version));
    }
}

public class InvoiceRecordPaymentHandler : IRequestHandler<InvoiceRecordPaymentCmd, InvoiceResponse>
{
    private readonly IInvoiceRepo _repo;
    private readonly IPaymentClearancePort _payment;
    private readonly IAptAuthorizationPolicy _auth;

    public InvoiceRecordPaymentHandler(IInvoiceRepo repo, IPaymentClearancePort payment, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _payment = payment;
        _auth = auth;
    }

    public Task<InvoiceResponse> Handle(InvoiceRecordPaymentCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(InvoiceRecordPaymentCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.PaymentClearanceReff, nameof(request.PaymentClearanceReff));
        var evidence = _payment.Load(request.PaymentClearanceReff)
                      ?? throw new ApotekDomainException("Payment Clearance is not inferred from Invoice status.");
        var invoice = _repo.LoadEntity(InvoiceModel.Key(request.InvoiceId))
            .GetValueOrThrow($"Invoice '{request.InvoiceId}' not found");
        invoice.AssertExpectedVersion(request.ExpectedVersion);
        invoice.RecordPaymentClearance(evidence.PaymentClearanceReff, evidence.ClearedAt);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(invoice);
        trans.Complete();
        return Task.FromResult(new InvoiceResponse(invoice.InvoiceId, invoice.InvoiceStatus, invoice.Version));
    }
}

public class InvoiceReviseHandler : IRequestHandler<InvoiceReviseCmd, InvoiceResponse>
{
    private readonly IInvoiceRepo _repo;
    private readonly ITataRekeningInvoicePermissionPort _permission;
    private readonly IAptAuthorizationPolicy _auth;

    public InvoiceReviseHandler(IInvoiceRepo repo, ITataRekeningInvoicePermissionPort permission, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _permission = permission;
        _auth = auth;
    }

    public Task<InvoiceResponse> Handle(InvoiceReviseCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(InvoiceReviseCmd), request.UserId);
        var invoice = _repo.LoadEntity(InvoiceModel.Key(request.InvoiceId))
            .GetValueOrThrow($"Invoice '{request.InvoiceId}' not found");
        invoice.AssertExpectedVersion(request.ExpectedVersion);
        var allowed = invoice.InvoiceStatus == InvoiceStatusEnum.Established
                      || _permission.AllowsModification(invoice.TataRekeningChargeId);
        invoice.RewriteContent(request.Items, request.Charges, request.DiskonLain, request.BiayaLain, request.Pembulatan, allowed);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(invoice);
        trans.Complete();
        return Task.FromResult(new InvoiceResponse(invoice.InvoiceId, invoice.InvoiceStatus, invoice.Version));
    }
}

public class InvoiceRecordCorrectionHandler : IRequestHandler<InvoiceRecordCorrectionCmd, InvoiceResponse>
{
    private readonly IInvoiceRepo _repo;
    private readonly IAptAuthorizationPolicy _auth;

    public InvoiceRecordCorrectionHandler(IInvoiceRepo repo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _auth = auth;
    }

    public Task<InvoiceResponse> Handle(InvoiceRecordCorrectionCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(InvoiceRecordCorrectionCmd), request.UserId);
        var invoice = _repo.LoadEntity(InvoiceModel.Key(request.InvoiceId))
            .GetValueOrThrow($"Invoice '{request.InvoiceId}' not found");
        invoice.AssertExpectedVersion(request.ExpectedVersion);
        invoice.RecordCorrectionReff(request.CorrectionReff);
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(invoice);
        trans.Complete();
        return Task.FromResult(new InvoiceResponse(invoice.InvoiceId, invoice.InvoiceStatus, invoice.Version));
    }
}
