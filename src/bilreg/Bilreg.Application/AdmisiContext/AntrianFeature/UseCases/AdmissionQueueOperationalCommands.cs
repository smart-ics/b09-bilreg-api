using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record AdmissionQueueOperationResponse(
    string AntrianId,
    int NoUrut,
    string Status);

public record AdmissionQueueCallCmd(
    string AntrianId,
    int NoUrut,
    string LoketKey,
    string UserId) : IRequest<AdmissionQueueOperationResponse>;

public record AdmissionQueueRecallCmd(
    string AntrianId,
    int NoUrut,
    string LoketKey,
    byte[] ExpectedRowVersion,
    string UserId) : IRequest<AdmissionQueueOperationResponse>;

public record AdmissionQueueReturnToWaitingCmd(
    string AntrianId,
    int NoUrut,
    string LoketKey,
    byte[] ExpectedRowVersion,
    string UserId) : IRequest<AdmissionQueueOperationResponse>;

public record AdmissionQueueStartServiceCmd(
    string AntrianId,
    int NoUrut,
    string LoketKey,
    byte[] ExpectedRowVersion,
    string UserId) : IRequest<AdmissionQueueOperationResponse>;

public record AdmissionQueueWithdrawCmd(
    string AntrianId,
    int NoUrut,
    string Reason,
    string? LoketKey,
    byte[]? ExpectedRowVersion,
    string UserId) : IRequest<AdmissionQueueOperationResponse>;

public record AdmissionQueueNoShowCmd(
    string AntrianId,
    int NoUrut,
    string LoketKey,
    byte[] ExpectedRowVersion,
    string UserId) : IRequest<AdmissionQueueOperationResponse>;

public record AdmissionQueueRedirectCmd(
    string AntrianId,
    int NoUrut,
    string TargetServicePointId,
    string? LoketKey,
    byte[]? ExpectedRowVersion,
    string UserId) : IRequest<AdmissionQueueOperationResponse>;

file static class AdmissionQueueOperationSupport
{
    public static void ValidateEntryRequest(string antrianId, int noUrut, string userId)
    {
        Guard.Against.NullOrWhiteSpace(antrianId);
        Guard.Against.NullOrWhiteSpace(userId);
        if (noUrut <= 0)
            throw new ArgumentOutOfRangeException(nameof(noUrut));
    }

    public static AntrianEntryModel RequireEntry(
        IAntrianRepo queues,
        string antrianId,
        int noUrut)
    {
        var queue = queues.LoadEntity(AntrianModel.Key(antrianId))
            .GetValueOrThrow($"Admission queue '{antrianId}' not found");

        return queue.ListEntry.FirstOrDefault(x => x.NoUrut == noUrut)
            ?? throw new KeyNotFoundException($"Queue entry '{antrianId}' / {noUrut} not found");
    }

    public static void ThrowConflict(string antrianId, int noUrut) =>
        throw new AdmissionQueueConcurrencyException(
            $"Queue entry '{antrianId}' / {noUrut} changed concurrently or its Loket claim is stale.");
}

public sealed class AdmissionQueueCallHandler
    : IRequestHandler<AdmissionQueueCallCmd, AdmissionQueueOperationResponse>
{
    private readonly IAntrianRepo _queues;
    private readonly IAdmissionQueueOperationRepo _operations;
    private readonly ITglJamProvider _clock;
    private readonly IAdmissionQueueRefreshPublisher _publisher;

    public AdmissionQueueCallHandler(
        IAntrianRepo queues,
        IAdmissionQueueOperationRepo operations,
        ITglJamProvider clock,
        IAdmissionQueueRefreshPublisher publisher)
    {
        _queues = queues;
        _operations = operations;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<AdmissionQueueOperationResponse> Handle(
        AdmissionQueueCallCmd request,
        CancellationToken cancellationToken)
    {
        AdmissionQueueOperationSupport.ValidateEntryRequest(
            request.AntrianId, request.NoUrut, request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey);

        AdmissionQueueOperationSupport.RequireEntry(_queues, request.AntrianId, request.NoUrut)
            .RecordCall();

        using (var trans = TransHelper.NewScope())
        {
            if (!_operations.TryCall(
                    request.AntrianId,
                    request.NoUrut,
                    request.LoketKey,
                    request.UserId,
                    _clock.Now))
                AdmissionQueueOperationSupport.ThrowConflict(request.AntrianId, request.NoUrut);

            trans.Complete();
        }

        await _publisher.PublishAsync(request.LoketKey, cancellationToken);
        return new AdmissionQueueOperationResponse(request.AntrianId, request.NoUrut, "Outstanding");
    }
}

public sealed class AdmissionQueueRecallHandler
    : IRequestHandler<AdmissionQueueRecallCmd, AdmissionQueueOperationResponse>
{
    private readonly IAntrianRepo _queues;
    private readonly IAdmissionQueueOperationRepo _operations;
    private readonly ITglJamProvider _clock;
    private readonly IAdmissionQueueRefreshPublisher _publisher;

    public AdmissionQueueRecallHandler(
        IAntrianRepo queues,
        IAdmissionQueueOperationRepo operations,
        ITglJamProvider clock,
        IAdmissionQueueRefreshPublisher publisher)
    {
        _queues = queues;
        _operations = operations;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<AdmissionQueueOperationResponse> Handle(
        AdmissionQueueRecallCmd request,
        CancellationToken cancellationToken)
    {
        AdmissionQueueOperationSupport.ValidateEntryRequest(
            request.AntrianId, request.NoUrut, request.UserId);

        AdmissionQueueOperationSupport.RequireEntry(_queues, request.AntrianId, request.NoUrut)
            .RecordCall();

        using (var trans = TransHelper.NewScope())
        {
            if (!_operations.TryRecall(
                    request.AntrianId,
                    request.NoUrut,
                    request.LoketKey,
                    request.ExpectedRowVersion,
                    request.UserId,
                    _clock.Now))
                AdmissionQueueOperationSupport.ThrowConflict(request.AntrianId, request.NoUrut);

            trans.Complete();
        }

        await _publisher.PublishAsync(request.LoketKey, cancellationToken);
        return new AdmissionQueueOperationResponse(request.AntrianId, request.NoUrut, "Outstanding");
    }
}

public sealed class AdmissionQueueReturnToWaitingHandler
    : IRequestHandler<AdmissionQueueReturnToWaitingCmd, AdmissionQueueOperationResponse>
{
    private const string AuditAction = "RETURN_TO_WAITING";
    private const string AuditEntity = "AdmissionQueueLoketClaim";
    private const string AuditDisposition = "UnansweredCall";
    private readonly IAntrianRepo _queues;
    private readonly IAdmissionQueueOperationRepo _operations;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _clock;
    private readonly IAdmissionQueueRefreshPublisher _publisher;

    public AdmissionQueueReturnToWaitingHandler(
        IAntrianRepo queues,
        IAdmissionQueueOperationRepo operations,
        IAuditRepo auditRepo,
        ITglJamProvider clock,
        IAdmissionQueueRefreshPublisher publisher)
    {
        _queues = queues;
        _operations = operations;
        _auditRepo = auditRepo;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<AdmissionQueueOperationResponse> Handle(
        AdmissionQueueReturnToWaitingCmd request,
        CancellationToken cancellationToken)
    {
        AdmissionQueueOperationSupport.ValidateEntryRequest(
            request.AntrianId, request.NoUrut, request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey);
        Guard.Against.NullOrEmpty(request.ExpectedRowVersion);

        var entry = AdmissionQueueOperationSupport.RequireEntry(
            _queues, request.AntrianId, request.NoUrut);
        if (entry.AntrianStatus != AntrianStatusEnum.Waiting)
            AdmissionQueueOperationSupport.ThrowConflict(request.AntrianId, request.NoUrut);

        var at = _clock.Now;
        var auditContext = AuditLogSnapshotJson.Serialize(new
        {
            request.LoketKey,
            request.AntrianId,
            request.NoUrut,
            ClaimState = AdmissionQueueClaimState.Outstanding.ToString(),
            ExpectedRowVersion = Convert.ToBase64String(request.ExpectedRowVersion)
        });

        using (var trans = TransHelper.NewScope())
        {
            if (!_operations.TryReturnToWaiting(
                    request.AntrianId,
                    request.NoUrut,
                    request.LoketKey,
                    request.ExpectedRowVersion,
                    request.UserId,
                    at))
                AdmissionQueueOperationSupport.ThrowConflict(request.AntrianId, request.NoUrut);

            _auditRepo.SaveChanges(AuditLog.Create(
                request.UserId,
                at,
                AuditAction,
                AuditEntity,
                $"{request.AntrianId}:{request.NoUrut}",
                AuditDisposition,
                auditContext));
            trans.Complete();
        }

        await _publisher.PublishAsync(request.LoketKey, cancellationToken);
        return new AdmissionQueueOperationResponse(request.AntrianId, request.NoUrut, "Waiting");
    }
}

public sealed class AdmissionQueueStartServiceHandler
    : IRequestHandler<AdmissionQueueStartServiceCmd, AdmissionQueueOperationResponse>
{
    private readonly IAntrianRepo _queues;
    private readonly IAdmissionQueueOperationRepo _operations;
    private readonly ITglJamProvider _clock;
    private readonly IAdmissionQueueRefreshPublisher _publisher;

    public AdmissionQueueStartServiceHandler(
        IAntrianRepo queues,
        IAdmissionQueueOperationRepo operations,
        ITglJamProvider clock,
        IAdmissionQueueRefreshPublisher publisher)
    {
        _queues = queues;
        _operations = operations;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<AdmissionQueueOperationResponse> Handle(
        AdmissionQueueStartServiceCmd request,
        CancellationToken cancellationToken)
    {
        AdmissionQueueOperationSupport.ValidateEntryRequest(
            request.AntrianId, request.NoUrut, request.UserId);

        var at = _clock.Now;
        AdmissionQueueOperationSupport.RequireEntry(_queues, request.AntrianId, request.NoUrut)
            .StartCalledService(at);

        using (var trans = TransHelper.NewScope())
        {
            if (!_operations.TryStartService(
                    request.AntrianId,
                    request.NoUrut,
                    request.LoketKey,
                    request.ExpectedRowVersion,
                    request.UserId,
                    at))
                AdmissionQueueOperationSupport.ThrowConflict(request.AntrianId, request.NoUrut);

            trans.Complete();
        }

        await _publisher.PublishAsync(request.LoketKey, cancellationToken);
        return new AdmissionQueueOperationResponse(request.AntrianId, request.NoUrut, "InService");
    }
}

public sealed class AdmissionQueueWithdrawHandler
    : IRequestHandler<AdmissionQueueWithdrawCmd, AdmissionQueueOperationResponse>
{
    private readonly IAntrianRepo _queues;
    private readonly IAdmissionQueueOperationRepo _operations;
    private readonly ITglJamProvider _clock;
    private readonly IAdmissionQueueRefreshPublisher _publisher;

    public AdmissionQueueWithdrawHandler(
        IAntrianRepo queues,
        IAdmissionQueueOperationRepo operations,
        ITglJamProvider clock,
        IAdmissionQueueRefreshPublisher publisher)
    {
        _queues = queues;
        _operations = operations;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<AdmissionQueueOperationResponse> Handle(
        AdmissionQueueWithdrawCmd request,
        CancellationToken cancellationToken)
    {
        AdmissionQueueOperationSupport.ValidateEntryRequest(
            request.AntrianId, request.NoUrut, request.UserId);

        var at = _clock.Now;
        AdmissionQueueOperationSupport.RequireEntry(_queues, request.AntrianId, request.NoUrut)
            .Withdraw(request.Reason, request.UserId, at);

        using (var trans = TransHelper.NewScope())
        {
            if (!_operations.TryWithdraw(
                    request.AntrianId,
                    request.NoUrut,
                    request.Reason,
                    request.UserId,
                    at,
                    request.LoketKey,
                    request.ExpectedRowVersion))
                AdmissionQueueOperationSupport.ThrowConflict(request.AntrianId, request.NoUrut);

            trans.Complete();
        }

        await _publisher.PublishAsync(request.LoketKey, cancellationToken);
        return new AdmissionQueueOperationResponse(request.AntrianId, request.NoUrut, "Withdrawn");
    }
}

public sealed class AdmissionQueueNoShowHandler
    : IRequestHandler<AdmissionQueueNoShowCmd, AdmissionQueueOperationResponse>
{
    private readonly IAntrianRepo _queues;
    private readonly IAdmissionQueueOperationRepo _operations;
    private readonly ITglJamProvider _clock;
    private readonly IAdmissionQueueRefreshPublisher _publisher;

    public AdmissionQueueNoShowHandler(
        IAntrianRepo queues,
        IAdmissionQueueOperationRepo operations,
        ITglJamProvider clock,
        IAdmissionQueueRefreshPublisher publisher)
    {
        _queues = queues;
        _operations = operations;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task<AdmissionQueueOperationResponse> Handle(
        AdmissionQueueNoShowCmd request,
        CancellationToken cancellationToken)
    {
        AdmissionQueueOperationSupport.ValidateEntryRequest(
            request.AntrianId, request.NoUrut, request.UserId);

        const string reason = "NoShow";
        var at = _clock.Now;
        AdmissionQueueOperationSupport.RequireEntry(_queues, request.AntrianId, request.NoUrut)
            .Withdraw(reason, request.UserId, at);

        using (var trans = TransHelper.NewScope())
        {
            if (!_operations.TryWithdraw(
                    request.AntrianId,
                    request.NoUrut,
                    reason,
                    request.UserId,
                    at,
                    request.LoketKey,
                    request.ExpectedRowVersion))
                AdmissionQueueOperationSupport.ThrowConflict(request.AntrianId, request.NoUrut);

            trans.Complete();
        }

        await _publisher.PublishAsync(request.LoketKey, cancellationToken);
        return new AdmissionQueueOperationResponse(request.AntrianId, request.NoUrut, "Withdrawn");
    }
}

public sealed class AdmissionQueueRedirectHandler
    : IRequestHandler<AdmissionQueueRedirectCmd, AdmissionQueueOperationResponse>
{
    private readonly IAntrianRepo _queues;
    private readonly IAdmissionQueueOperationRepo _operations;
    private readonly ITglJamProvider _clock;
    private readonly IAdmissionQueueRefreshPublisher _publisher;
    private readonly IAdmissionServicePointRepo _servicePoints;
    private readonly IAntrianFactory _factory;

    public AdmissionQueueRedirectHandler(
        IAntrianRepo queues,
        IAdmissionQueueOperationRepo operations,
        ITglJamProvider clock,
        IAdmissionQueueRefreshPublisher publisher,
        IAdmissionServicePointRepo servicePoints,
        IAntrianFactory factory)
    {
        _queues = queues;
        _operations = operations;
        _clock = clock;
        _publisher = publisher;
        _servicePoints = servicePoints;
        _factory = factory;
    }

    public async Task<AdmissionQueueOperationResponse> Handle(
        AdmissionQueueRedirectCmd request,
        CancellationToken cancellationToken)
    {
        AdmissionQueueOperationSupport.ValidateEntryRequest(
            request.AntrianId, request.NoUrut, request.UserId);

        var at = _clock.Now;
        var origin = AdmissionQueueOperationSupport.RequireEntry(
            _queues, request.AntrianId, request.NoUrut);
        origin.Withdraw("Redirected", request.UserId, at);

        var point = _servicePoints
            .LoadEntity(AdmissionServicePointModel.Key(request.TargetServicePointId))
            .GetValueOrThrow($"Admission Service Point '{request.TargetServicePointId}' not found");
        point.EnsureCanAcceptIntake();

        var businessDate = DateOnly.FromDateTime(at);
        var reference = new ServicePointType(point.ServicePointId, point.DisplayName);
        var sequenceTag = AntrianModel.GenSequenceTag(businessDate, TimeOnly.MinValue, reference);
        var existing = _queues.ListData(businessDate)
            .FirstOrDefault(x => x.SequenceTag == sequenceTag);
        var target = existing is null
            ? _factory.Create(point, businessDate)
            : _queues.LoadEntity(existing).Value;

        var replacement = target.AddAdmissionEntry(at);
        replacement.MarkRedirectReplacement(request.AntrianId, request.NoUrut);

        using (var trans = TransHelper.NewScope())
        {
            if (!_operations.TryRedirect(
                    request.AntrianId,
                    request.NoUrut,
                    request.UserId,
                    at,
                    request.LoketKey,
                    request.ExpectedRowVersion,
                    target,
                    replacement))
                AdmissionQueueOperationSupport.ThrowConflict(request.AntrianId, request.NoUrut);

            trans.Complete();
        }

        await _publisher.PublishAsync(request.LoketKey, cancellationToken);
        return new AdmissionQueueOperationResponse(target.AntrianId, replacement.NoUrut, "Waiting");
    }
}
