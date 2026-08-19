using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.CopyResepFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.JualBebasFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.JualBebasFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;

public record SalesOrderEstablishCmd(
    string UserId,
    SalesOrderSourceKindEnum SourceKind,
    string SourceId,
    PayerPathEnum PayerPath,
    PartialReasonEnum PartialReason,
    List<SalesOrderEstablishItem>? QtyOverrides)
    : IRequest<SalesOrderEstablishResponse>;

public record SalesOrderEstablishItem(int SourceItemNo, decimal AcceptedQty);

public record SalesOrderEstablishResponse(string SalesOrderId, int Version, string? CopyResepId);

public record SalesOrderAppendUnfulfilledCmd(
    string UserId,
    string SalesOrderId,
    int ExpectedVersion,
    int ItemNo,
    decimal Qty,
    UnfulfilledReasonEnum Reason)
    : IRequest<SalesOrderEstablishResponse>;

public class SalesOrderEstablishHandler : IRequestHandler<SalesOrderEstablishCmd, SalesOrderEstablishResponse>
{
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly ITelaahResepRepo _telaahRepo;
    private readonly IResepKerjaRepo _resepRepo;
    private readonly IJualBebasRepo _jualBebasRepo;
    private readonly IAvailableStockPort _availableStock;
    private readonly ICopyResepRepo _copyResepRepo;
    private readonly IAptIntegrationTaskRepo _taskRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public SalesOrderEstablishHandler(
        ISalesOrderRepo salesOrderRepo,
        ITelaahResepRepo telaahRepo,
        IResepKerjaRepo resepRepo,
        IJualBebasRepo jualBebasRepo,
        IAvailableStockPort availableStock,
        ICopyResepRepo copyResepRepo,
        IAptIntegrationTaskRepo taskRepo,
        IAptAuthorizationPolicy auth)
    {
        _salesOrderRepo = salesOrderRepo;
        _telaahRepo = telaahRepo;
        _resepRepo = resepRepo;
        _jualBebasRepo = jualBebasRepo;
        _availableStock = availableStock;
        _copyResepRepo = copyResepRepo;
        _taskRepo = taskRepo;
        _auth = auth;
    }

    public Task<SalesOrderEstablishResponse> Handle(SalesOrderEstablishCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(SalesOrderEstablishCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.SourceId, nameof(request.SourceId));

        string regId;
        string pasienId;
        string pasienName;
        string telaahId = "";
        string resepKerjaId = "";
        int iterEntitled = 0;
        string sourceResepId = "";
        var proposed = new List<SalesOrderItemModel>();
        var components = new List<SalesOrderItemComponentModel>();
        var excluded = new List<(int ItemNo, string BrgId, decimal Qty)>();

        if (request.SourceKind == SalesOrderSourceKindEnum.ResepKerja)
        {
            var resep = _resepRepo.LoadEntity(ResepKerjaModel.Key(request.SourceId))
                .GetValueOrThrow($"Resep Kerja '{request.SourceId}' not found");
            var telaah = _telaahRepo.LoadByResepKerja(resep.ResepKerjaId)
                .GetValueOrThrow($"Telaah for '{request.SourceId}' not found");
            if (!telaah.CanEstablishSalesOrder)
                throw new ApotekDomainException("Rejected or incomplete Telaah cannot establish a Sales Order.");
            regId = resep.RegId;
            pasienId = resep.PasienId;
            pasienName = resep.PasienName;
            telaahId = telaah.TelaahResepId;
            resepKerjaId = resep.ResepKerjaId;
            iterEntitled = resep.IterEntitled;
            sourceResepId = resep.SourceResepId;
            foreach (var line in telaah.AcceptedItems())
            {
                var qty = request.QtyOverrides?.FirstOrDefault(x => x.SourceItemNo == line.ResepKerjaItemNo)?.AcceptedQty
                          ?? line.AcceptedQty;
                if (qty <= 0)
                {
                    excluded.Add((line.ResepKerjaItemNo, line.AcceptedBrgId, line.AcceptedQty));
                    continue;
                }
                proposed.Add(SalesOrderItemModel.Establish(
                    proposed.Count + 1, line.ResepKerjaItemNo, line.AcceptedBrgId, line.AcceptedBrgName,
                    "", qty, FornasCoverageEnum.Unknown, "", false));
            }

            foreach (var telaahItem in telaah.Items.Where(x => !x.IsAccepted))
                excluded.Add((telaahItem.ResepKerjaItemNo, telaahItem.AcceptedBrgId, telaahItem.AcceptedQty == 0 ? 1 : telaahItem.AcceptedQty));
        }
        else
        {
            var jb = _jualBebasRepo.LoadEntity(JualBebasModel.Key(request.SourceId))
                .GetValueOrThrow($"Jual Bebas '{request.SourceId}' not found");
            if (jb.RequestStatus != JualBebasRequestStatusEnum.Accepted)
                throw new ApotekDomainException("Only an accepted Jual Bebas can establish a Sales Order.");
            regId = jb.RegId;
            pasienId = jb.PasienId;
            pasienName = jb.PasienName;
            foreach (var line in jb.Items)
            {
                proposed.Add(SalesOrderItemModel.Establish(
                    proposed.Count + 1, line.ItemNo, line.BrgId, line.BrgName, line.SatuanId, line.Qty,
                    FornasCoverageEnum.Unknown, "", false));
            }
        }

        var existing = _salesOrderRepo.LoadActive(request.SourceKind, request.SourceId, regId, request.PayerPath);
        if (existing.HasValue)
            return Task.FromResult(new SalesOrderEstablishResponse(existing.Value.SalesOrderId, existing.Value.Version, null));

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
            {
                excluded.Add((item.SourceItemNo, item.BrgId, item.AcceptedQty));
                continue;
            }
            if (take < item.AcceptedQty)
                excluded.Add((item.SourceItemNo, item.BrgId, item.AcceptedQty - take));
            accepted.Add(SalesOrderItemModel.Establish(
                accepted.Count + 1, item.SourceItemNo, item.BrgId, item.BrgName, item.SatuanId, take,
                item.FornasCoverage, item.SepNo, item.IsRacik));
        }

        if (accepted.Count == 0)
            throw new ApotekDomainException("No positive Accepted Qty remains after Available Stock evaluation.");

        var partial = excluded.Count == 0 ? PartialReasonEnum.None : request.PartialReason;
        if (partial == PartialReasonEnum.None && excluded.Count > 0)
            partial = PartialReasonEnum.StockShortage;

        var order = SalesOrderModel.Establish(
            request.SourceKind, request.SourceId, telaahId, regId, pasienId, pasienName,
            request.PayerPath, partial, accepted, components);

        Domain.ApotekContext.CopyResepFeature.CopyResepModel? copy = null;
        if (excluded.Count > 0)
        {
            copy = Domain.ApotekContext.CopyResepFeature.CopyResepModel.Issue(
                resepKerjaId,
                order.SalesOrderId,
                (int)partial,
                request.UserId,
                DateTime.Now,
                excluded.Select((x, i) => new Domain.ApotekContext.CopyResepFeature.CopyResepItemModel(
                    i + 1, x.ItemNo, x.BrgId, x.Qty, "")));
        }

        if (request.SourceKind == SalesOrderSourceKindEnum.JualBebas)
        {
            var jb = _jualBebasRepo.LoadEntity(JualBebasModel.Key(request.SourceId)).Value;
            jb.MarkConvertedToSalesOrder();
            using var transJb = TransHelper.NewScope();
            _salesOrderRepo.SaveChanges(order);
            if (copy is not null)
                _copyResepRepo.SaveChanges(copy);
            _jualBebasRepo.SaveChanges(jb);
            transJb.Complete();
            return Task.FromResult(new SalesOrderEstablishResponse(order.SalesOrderId, order.Version, copy?.CopyResepId));
        }

        using (var trans = TransHelper.NewScope())
        {
            _salesOrderRepo.SaveChanges(order);
            if (copy is not null)
                _copyResepRepo.SaveChanges(copy);
            if (iterEntitled > 0 && !string.IsNullOrWhiteSpace(sourceResepId))
            {
                AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
                    AptIntegrationTaskTypeEnum.IterConsume,
                    AptIntegrationSourceKindEnum.SalesOrder,
                    order.SalesOrderId,
                    $"{order.SalesOrderId}:ITER",
                    AptIntegrationDestinationEnum.ResepIter,
                    System.Text.Json.JsonSerializer.Serialize(new { sourceResepId, consumeCount = 1 })));
            }
            trans.Complete();
        }

        return Task.FromResult(new SalesOrderEstablishResponse(order.SalesOrderId, order.Version, copy?.CopyResepId));
    }
}

public class SalesOrderAppendUnfulfilledHandler : IRequestHandler<SalesOrderAppendUnfulfilledCmd, SalesOrderEstablishResponse>
{
    private readonly ISalesOrderRepo _repo;
    private readonly ICopyResepRepo _copyRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public SalesOrderAppendUnfulfilledHandler(ISalesOrderRepo repo, ICopyResepRepo copyRepo, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _copyRepo = copyRepo;
        _auth = auth;
    }

    public Task<SalesOrderEstablishResponse> Handle(SalesOrderAppendUnfulfilledCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(SalesOrderAppendUnfulfilledCmd), request.UserId);
        var order = _repo.LoadEntity(SalesOrderModel.Key(request.SalesOrderId))
            .GetValueOrThrow($"Sales Order '{request.SalesOrderId}' not found");
        order.AssertExpectedVersion(request.ExpectedVersion);
        var item = order.Item(request.ItemNo);
        var copy = Domain.ApotekContext.CopyResepFeature.CopyResepModel.Issue(
            order.SourceKind == SalesOrderSourceKindEnum.ResepKerja ? order.SourceId : "",
            order.SalesOrderId,
            (int)PartialReasonEnum.StockShortage,
            request.UserId,
            DateTime.Now,
            [new Domain.ApotekContext.CopyResepFeature.CopyResepItemModel(1, item.SourceItemNo, item.BrgId, request.Qty, "post-SO")]);
        order.AppendUnfulfilled(request.ItemNo, request.Qty, request.Reason, copy.CopyResepId, request.UserId, DateTime.Now);
        using var trans = TransHelper.NewScope();
        _copyRepo.SaveChanges(copy);
        _repo.SaveChanges(order);
        trans.Complete();
        return Task.FromResult(new SalesOrderEstablishResponse(order.SalesOrderId, order.Version, copy.CopyResepId));
    }
}
