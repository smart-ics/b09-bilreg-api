using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;

public record SalesOrderEstablishMixedCmd(string UserId, string ResepKerjaId, string SepNo)
    : IRequest<SalesOrderEstablishMixedResponse>;

public record SalesOrderEstablishMixedResponse(
    string? BpjsSalesOrderId,
    int? BpjsVersion,
    string? PatientPaySalesOrderId,
    int? PatientPayVersion);

public record MixedCoverageReadQuery(string ResepKerjaId) : IRequest<MixedCoverageReadResponse>;

public record MixedCoverageItemRead(
    int ItemNo,
    int SourceItemNo,
    string BrgId,
    decimal AcceptedQty,
    FornasCoverageEnum FornasCoverage,
    string SepNo);

public record MixedCoverageOrderArm(
    string SalesOrderId,
    int Version,
    PayerPathEnum PayerPath,
    SalesOrderStatusEnum Status,
    PartialReasonEnum PartialReason,
    string? InvoiceId,
    InvoiceStatusEnum? InvoiceStatus,
    bool HasPaymentClearance,
    IReadOnlyList<MixedCoverageItemRead> Items);

public record MixedCoverageReadResponse(
    string ResepKerjaId,
    MixedCoverageOrderArm? BpjsOrder,
    MixedCoverageOrderArm? PatientPayOrder);

public class SalesOrderEstablishMixedHandler : IRequestHandler<SalesOrderEstablishMixedCmd, SalesOrderEstablishMixedResponse>
{
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly ITelaahResepRepo _telaahRepo;
    private readonly IResepKerjaRepo _resepRepo;
    private readonly IAvailableStockPort _availableStock;
    private readonly IAptIntegrationTaskRepo _taskRepo;
    private readonly ISepFornasPort _sepFornas;
    private readonly IAptAuthorizationPolicy _auth;

    public SalesOrderEstablishMixedHandler(
        ISalesOrderRepo salesOrderRepo,
        ITelaahResepRepo telaahRepo,
        IResepKerjaRepo resepRepo,
        IAvailableStockPort availableStock,
        IAptIntegrationTaskRepo taskRepo,
        ISepFornasPort sepFornas,
        IAptAuthorizationPolicy auth)
    {
        _salesOrderRepo = salesOrderRepo;
        _telaahRepo = telaahRepo;
        _resepRepo = resepRepo;
        _availableStock = availableStock;
        _taskRepo = taskRepo;
        _sepFornas = sepFornas;
        _auth = auth;
    }

    public Task<SalesOrderEstablishMixedResponse> Handle(SalesOrderEstablishMixedCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(SalesOrderEstablishMixedCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.ResepKerjaId, nameof(request.ResepKerjaId));
        if (string.IsNullOrWhiteSpace(request.SepNo))
            throw new ApotekDomainException("Mixed coverage requires a valid SEP.");
        if (request.SepNo.Length > 50)
            throw new ApotekDomainException("SepNo exceeds PD-03 width 50.");

        var resep = _resepRepo.LoadEntity(ResepKerjaModel.Key(request.ResepKerjaId))
            .GetValueOrThrow($"Resep Kerja '{request.ResepKerjaId}' not found");
        var telaah = _telaahRepo.LoadByResepKerja(resep.ResepKerjaId)
            .GetValueOrThrow($"Telaah for '{request.ResepKerjaId}' not found");
        if (!telaah.CanEstablishSalesOrder)
            throw new ApotekDomainException("Rejected or incomplete Telaah cannot establish mixed Sales Orders.");

        var coveredLines = new List<TelaahResepItemModel>();
        var notCoveredLines = new List<TelaahResepItemModel>();
        foreach (var line in telaah.AcceptedItems())
        {
            var evidence = _sepFornas.Evaluate(resep.RegId, line.AcceptedBrgId);
            var coverage = evidence.Coverage;
            if (coverage == FornasCoverageEnum.Unknown)
                throw new ApotekDomainException("Fornas master membership alone is not Coverage Clearance.");
            if (coverage == FornasCoverageEnum.Covered)
                coveredLines.Add(line);
            else
                notCoveredLines.Add(line);
        }

        if (coveredLines.Count == 0 || notCoveredLines.Count == 0)
            throw new ApotekDomainException("Mixed coverage requires both Covered and Not Covered reviewed items.");

        var existingBpjs = _salesOrderRepo.LoadActive(
            SalesOrderSourceKindEnum.ResepKerja, request.ResepKerjaId, resep.RegId, PayerPathEnum.Bpjs);
        var existingPatientPay = _salesOrderRepo.LoadActive(
            SalesOrderSourceKindEnum.ResepKerja, request.ResepKerjaId, resep.RegId, PayerPathEnum.GeneralPatientPay);
        if (existingBpjs.HasValue && existingPatientPay.HasValue)
        {
            return Task.FromResult(new SalesOrderEstablishMixedResponse(
                existingBpjs.Value.SalesOrderId,
                existingBpjs.Value.Version,
                existingPatientPay.Value.SalesOrderId,
                existingPatientPay.Value.Version));
        }

        var bpjsOrder = existingBpjs.HasValue
            ? existingBpjs.Value
            : EstablishArm(
                resep, telaah, coveredLines, PayerPathEnum.Bpjs, PartialReasonEnum.None, request.SepNo);
        var patientPayOrder = existingPatientPay.HasValue
            ? existingPatientPay.Value
            : EstablishArm(
                resep, telaah, notCoveredLines, PayerPathEnum.GeneralPatientPay, PartialReasonEnum.FornasNotCovered, request.SepNo);

        AssertReconcilesToReviewedDemand(telaah, bpjsOrder, patientPayOrder);

        using var trans = TransHelper.NewScope();
        if (!existingBpjs.HasValue)
            _salesOrderRepo.SaveChanges(bpjsOrder);
        if (!existingPatientPay.HasValue)
            _salesOrderRepo.SaveChanges(patientPayOrder);
        if (!existingBpjs.HasValue && resep.IterEntitled > 0 && !string.IsNullOrWhiteSpace(resep.SourceResepId))
        {
            AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
                AptIntegrationTaskTypeEnum.IterConsume,
                AptIntegrationSourceKindEnum.SalesOrder,
                bpjsOrder.SalesOrderId,
                $"{bpjsOrder.SalesOrderId}:ITER",
                AptIntegrationDestinationEnum.ResepIter,
                System.Text.Json.JsonSerializer.Serialize(new { SourceResepId = resep.SourceResepId, ConsumeCount = 1 })));
        }
        trans.Complete();

        return Task.FromResult(new SalesOrderEstablishMixedResponse(
            bpjsOrder.SalesOrderId,
            bpjsOrder.Version,
            patientPayOrder.SalesOrderId,
            patientPayOrder.Version));
    }

    private SalesOrderModel EstablishArm(
        ResepKerjaModel resep,
        TelaahResepModel telaah,
        IReadOnlyList<TelaahResepItemModel> lines,
        PayerPathEnum payerPath,
        PartialReasonEnum partialReason,
        string sepNo)
    {
        var proposed = new List<SalesOrderItemModel>();
        foreach (var line in lines)
        {
            proposed.Add(SalesOrderItemModel.Establish(
                proposed.Count + 1,
                line.ResepKerjaItemNo,
                line.AcceptedBrgId,
                line.AcceptedBrgName,
                "",
                line.AcceptedQty,
                payerPath == PayerPathEnum.Bpjs ? FornasCoverageEnum.Covered : FornasCoverageEnum.NotCovered,
                payerPath == PayerPathEnum.Bpjs ? sepNo : "",
                false));
        }

        var stock = _availableStock.Evaluate(
            proposed.Select(x => new AvailableStockRequest(x.BrgId, ApotekLocationIds.PharmacyUnitLayananId, x.AcceptedQty)).ToList());
        if (!stock.Evaluated)
            throw new AvailableStockUnavailableException();

        var accepted = new List<SalesOrderItemModel>();
        foreach (var item in proposed)
        {
            var available = stock.QtyFor(item.BrgId);
            var take = Math.Min(item.AcceptedQty, available);
            if (take <= 0)
                throw new ApotekDomainException("No positive Accepted Qty remains after Available Stock evaluation.");
            accepted.Add(SalesOrderItemModel.Establish(
                accepted.Count + 1,
                item.SourceItemNo,
                item.BrgId,
                item.BrgName,
                item.SatuanId,
                take,
                item.FornasCoverage,
                item.SepNo,
                item.IsRacik));
        }

        return SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.ResepKerja,
            resep.ResepKerjaId,
            telaah.TelaahResepId,
            resep.RegId,
            resep.PasienId,
            resep.PasienName,
            payerPath,
            partialReason,
            accepted,
            []);
    }

    private static void AssertReconcilesToReviewedDemand(
        TelaahResepModel telaah,
        SalesOrderModel bpjsOrder,
        SalesOrderModel patientPayOrder)
    {
        foreach (var line in telaah.AcceptedItems())
        {
            var bpjsQty = bpjsOrder.Items.Where(x => x.SourceItemNo == line.ResepKerjaItemNo).Sum(x => x.AcceptedQty);
            var patientQty = patientPayOrder.Items.Where(x => x.SourceItemNo == line.ResepKerjaItemNo).Sum(x => x.AcceptedQty);
            if (bpjsQty > 0 && patientQty > 0)
                throw new ApotekDomainException("Covered and Not Covered quantities must be mutually exclusive per reviewed line.");
            if (bpjsQty + patientQty != line.AcceptedQty)
                throw new ApotekDomainException("Mixed Sales Orders must reconcile to reviewed demand quantities.");
        }
    }
}

public class MixedCoverageReadHandler : IRequestHandler<MixedCoverageReadQuery, MixedCoverageReadResponse>
{
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IInvoiceRepo _invoiceRepo;

    public MixedCoverageReadHandler(ISalesOrderRepo salesOrderRepo, IInvoiceRepo invoiceRepo)
    {
        _salesOrderRepo = salesOrderRepo;
        _invoiceRepo = invoiceRepo;
    }

    public Task<MixedCoverageReadResponse> Handle(MixedCoverageReadQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ResepKerjaId, nameof(request.ResepKerjaId));
        var orders = _salesOrderRepo
            .ListBySource(SalesOrderSourceKindEnum.ResepKerja, request.ResepKerjaId)
            .Where(x => x.IsActiveKey || x.SalesOrderStatus == SalesOrderStatusEnum.Resolved)
            .ToList();

        return Task.FromResult(new MixedCoverageReadResponse(
            request.ResepKerjaId,
            ToArm(orders.FirstOrDefault(x => x.PayerPath == PayerPathEnum.Bpjs)),
            ToArm(orders.FirstOrDefault(x => x.PayerPath == PayerPathEnum.GeneralPatientPay))));
    }

    private MixedCoverageOrderArm? ToArm(SalesOrderModel? order)
    {
        if (order is null)
            return null;
        var invoice = _invoiceRepo.LoadActiveBySalesOrder(order.SalesOrderId);
        return new MixedCoverageOrderArm(
            order.SalesOrderId,
            order.Version,
            order.PayerPath,
            order.SalesOrderStatus,
            order.PartialReason,
            invoice.HasValue ? invoice.Value.InvoiceId : null,
            invoice.HasValue ? invoice.Value.InvoiceStatus : null,
            invoice.HasValue && invoice.Value.HasPaymentClearance,
            order.Items.Select(x => new MixedCoverageItemRead(
                x.ItemNo, x.SourceItemNo, x.BrgId, x.AcceptedQty, x.FornasCoverage, x.SepNo)).ToList());
    }
}
