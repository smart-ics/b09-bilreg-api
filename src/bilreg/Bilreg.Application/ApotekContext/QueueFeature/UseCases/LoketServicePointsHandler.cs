using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

#region Command & Query Records

/// <summary>
/// Command pembaruan pemetaan service point yang dilayani oleh suatu loket oleh Administrator.
/// </summary>
public record UpdateLoketServicePointsCmd(
    string LoketKey,
    IReadOnlyList<string> ServicePointIds,
    byte[] RowVersion,
    string UserId = "") : IRequest<LoketServicePointsResponse>
{
    public UpdateLoketServicePointsCmd(string loketKey, UpdateLoketServicePointsReq req)
        : this(loketKey, req.ServicePointIds, req.RowVersion, req.UserId)
    {
    }
}

/// <summary>
/// Query membaca daftar service point yang dilayani oleh suatu loket.
/// </summary>
public record GetLoketServicePointsQry(string LoketKey) : IRequest<LoketServicePointsResponse>;

#endregion

#region Handlers

/// <summary>
/// Handler untuk command pembaruan pemetaan service point loket fisik oleh Administrator.
/// Melindungi konkurensi dengan memvalidasi token ROWVERSION pada BILRG_AptLoketCurrentCall.
/// </summary>
public class UpdateLoketServicePointsCmdHandler
    : IRequestHandler<UpdateLoketServicePointsCmd, LoketServicePointsResponse>
{
    private readonly IPharmacyLoketServicePointRepo _loketServicePointRepo;
    private readonly IPharmacyQueueOperationRepo _operations;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public UpdateLoketServicePointsCmdHandler(
        IPharmacyLoketServicePointRepo loketServicePointRepo,
        IPharmacyQueueOperationRepo operations,
        IPharmacyQueuePublisher publisher,
        IAuditRepo auditRepo,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _loketServicePointRepo = loketServicePointRepo;
        _operations = operations;
        _publisher = publisher;
        _auditRepo = auditRepo;
        _clock = clock;
        _auth = auth;
    }

    public async Task<LoketServicePointsResponse> Handle(
        UpdateLoketServicePointsCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(UpdateLoketServicePointsCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));
        Guard.Against.NullOrEmpty(request.RowVersion, nameof(request.RowVersion));

        var at = _clock.Now;
        var actor = string.IsNullOrWhiteSpace(request.UserId) ? "SYSTEM" : request.UserId;

        var loket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (!loket.RowVersion.SequenceEqual(request.RowVersion))
        {
            throw new ApotekConcurrencyException(
                request.LoketKey,
                $"Failed to update service points for loket '{request.LoketKey}'. Token is stale or loket state has changed.");
        }

        _loketServicePointRepo.ReplaceServicePoints(request.LoketKey, request.ServicePointIds, actor, at);

        var auditContext = AuditLogSnapshotJson.Serialize(new
        {
            request.LoketKey,
            ServicePointIds = request.ServicePointIds,
            ExpectedRowVersion = Convert.ToBase64String(request.RowVersion),
            UserId = actor
        });

        _auditRepo.SaveChanges(AuditLog.Create(
            actor,
            at,
            "UPDATE_LOKET_SERVICE_POINTS",
            "PharmacyLoketServicePoint",
            request.LoketKey,
            "UpdateLoketServicePoints",
            auditContext));

        await _publisher.PublishWorklistRefresh(loket.DepoFarmasiId, cancellationToken);

        var updatedServicePoints = _loketServicePointRepo.ListServicePointsByLoket(request.LoketKey);

        return new LoketServicePointsResponse(
            request.LoketKey,
            updatedServicePoints,
            loket.RowVersion);
    }
}

/// <summary>
/// Handler untuk query pembacaan daftar service point yang dilayani oleh suatu loket.
/// </summary>
public class GetLoketServicePointsQryHandler
    : IRequestHandler<GetLoketServicePointsQry, LoketServicePointsResponse>
{
    private readonly IPharmacyLoketServicePointRepo _loketServicePointRepo;
    private readonly IPharmacyQueueOperationRepo _operations;

    public GetLoketServicePointsQryHandler(
        IPharmacyLoketServicePointRepo loketServicePointRepo,
        IPharmacyQueueOperationRepo operations)
    {
        _loketServicePointRepo = loketServicePointRepo;
        _operations = operations;
    }

    public Task<LoketServicePointsResponse> Handle(
        GetLoketServicePointsQry request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));

        var loket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        var servicePoints = _loketServicePointRepo.ListServicePointsByLoket(request.LoketKey);

        return Task.FromResult(new LoketServicePointsResponse(
            request.LoketKey,
            servicePoints,
            loket.RowVersion));
    }
}

#endregion
