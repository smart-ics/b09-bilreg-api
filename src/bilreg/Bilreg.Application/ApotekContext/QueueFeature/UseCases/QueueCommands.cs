using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.JualBebasFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.JualBebasFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

public record QueueMapCmd(
    string UserId,
    QueueDemandKindEnum DemandKind,
    string DemandId,
    string AntrianId,
    int NoUrut,
    string PasienTrackerId,
    QueueMappingMethodEnum MappingMethod)
    : IRequest<QueueMapResponse>;

public record QueueMapResponse(QueueDemandKindEnum DemandKind, string DemandId, string AntrianId, int NoUrut);

public record QueueCloseCmd(string UserId, string AntrianId, int NoUrut, string Reason)
    : IRequest<QueueCloseResponse>;

public record QueueCloseResponse(string QueueCloseId);

public class QueueMapHandler : IRequestHandler<QueueMapCmd, QueueMapResponse>
{
    private readonly IQueueMappingRepo _repo;
    private readonly IResepKerjaRepo _resepRepo;
    private readonly IJualBebasRepo _jualBebasRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public QueueMapHandler(
        IQueueMappingRepo repo,
        IResepKerjaRepo resepRepo,
        IJualBebasRepo jualBebasRepo,
        IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _resepRepo = resepRepo;
        _jualBebasRepo = jualBebasRepo;
        _auth = auth;
    }

    public Task<QueueMapResponse> Handle(QueueMapCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(QueueMapCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.DemandId, nameof(request.DemandId));
        if (request.DemandKind == QueueDemandKindEnum.ResepKerja)
            _resepRepo.LoadEntity(ResepKerjaModel.Key(request.DemandId))
                .GetValueOrThrow($"Resep Kerja '{request.DemandId}' not found");
        else
            _jualBebasRepo.LoadEntity(JualBebasModel.Key(request.DemandId))
                .GetValueOrThrow($"Jual Bebas '{request.DemandId}' not found");

        var existing = _repo.LoadEntity(QueueMappingModel.Key(request.DemandKind, request.DemandId));
        QueueMappingModel model;
        if (existing.HasValue)
        {
            model = existing.Value;
            model.Correct(request.AntrianId, request.NoUrut, request.PasienTrackerId, request.UserId, DateTime.Now);
        }
        else
        {
            model = QueueMappingModel.Create(
                request.DemandKind, request.DemandId, request.AntrianId, request.NoUrut,
                request.PasienTrackerId, request.MappingMethod, request.UserId, DateTime.Now);
        }

        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(model);
        trans.Complete();
        return Task.FromResult(new QueueMapResponse(model.DemandKind, model.DemandId, model.AntrianId, model.NoUrut));
    }
}

public class QueueCloseHandler : IRequestHandler<QueueCloseCmd, QueueCloseResponse>
{
    private readonly IQueueCloseRepo _closeRepo;
    private readonly ITrackerPharmacyPort _tracker;
    private readonly IAptIntegrationTaskRepo _taskRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public QueueCloseHandler(
        IQueueCloseRepo closeRepo,
        ITrackerPharmacyPort tracker,
        IAptIntegrationTaskRepo taskRepo,
        IAptAuthorizationPolicy auth)
    {
        _closeRepo = closeRepo;
        _tracker = tracker;
        _taskRepo = taskRepo;
        _auth = auth;
    }

    public Task<QueueCloseResponse> Handle(QueueCloseCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(QueueCloseCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        if (_closeRepo.LoadByQueue(request.AntrianId, request.NoUrut).HasValue)
            throw new InvalidOperationException("Queue is already closed.");
        if (_tracker.GetStatus(request.AntrianId, request.NoUrut) != AntrianStatusEnum.Waiting)
            throw new InvalidOperationException("Pharmacy Queue Close is allowed only from Waiting.");

        var close = QueueCloseModel.Create(request.AntrianId, request.NoUrut, request.Reason, request.UserId, DateTime.Now);
        using var trans = TransHelper.NewScope();
        _closeRepo.SaveChanges(close);
        AptIntegrationTaskEnqueue.InsertIfAbsent(_taskRepo, AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerWithdrawn,
            AptIntegrationSourceKindEnum.QueueClose,
            close.QueueCloseId,
            $"{close.QueueCloseId}:WDN",
            AptIntegrationDestinationEnum.Tracker,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                request.AntrianId,
                request.NoUrut,
                request.Reason,
                request.UserId
            })));
        trans.Complete();
        return Task.FromResult(new QueueCloseResponse(close.QueueCloseId));
    }
}
