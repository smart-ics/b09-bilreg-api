using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

#region Command Records

/// <summary>
/// Command pemanggilan tiket antrean berikutnya atau pemanggilan nomor spesifik pada Loket 1 (Penerimaan).
/// </summary>
public record PharmacyQueueCallPenerimaanCmd(
    string LoketKey,
    string? AntrianId = null,
    int? NoUrut = null,
    byte[]? ExpectedRowVersion = null,
    string UserId = "") : IRequest<PharmacyQueueLoketCallResponse>
{
    public PharmacyQueueCallPenerimaanCmd(PharmacyQueueCallReq req)
        : this(req.LoketKey, req.AntrianId, req.NoUrut, req.ExpectedRowVersion, req.UserId)
    {
    }
}

/// <summary>
/// Command pemanggilan ulang (Recall) nomor antrean aktif pada Loket 1 (Penerimaan).
/// </summary>
public record PharmacyQueueRecallPenerimaanCmd(
    string LoketKey,
    byte[] RowVersion,
    string UserId = "") : IRequest<PharmacyQueueLoketCallResponse>
{
    public PharmacyQueueRecallPenerimaanCmd(PharmacyQueueRecallReq req)
        : this(req.LoketKey, req.RowVersion, req.UserId)
    {
    }
}

/// <summary>
/// Command melewati (Skip) antrean yang tidak merespons panggilan pada Loket 1 (Penerimaan).
/// </summary>
public record PharmacyQueueSkipPenerimaanCmd(
    string LoketKey,
    string Reason,
    byte[] RowVersion,
    string UserId = "") : IRequest<PharmacyQueueLoketCallResponse>
{
    public PharmacyQueueSkipPenerimaanCmd(PharmacyQueueSkipReq req)
        : this(req.LoketKey, req.Reason, req.RowVersion, req.UserId)
    {
    }
}

/// <summary>
/// Command menandai dimulainya pelayanan tatap muka pada Loket 1 (Penerimaan / InService).
/// </summary>
public record PharmacyQueueStartPenerimaanCmd(
    string LoketKey,
    byte[] RowVersion,
    string UserId = "") : IRequest<PharmacyQueueLoketCallResponse>
{
    public PharmacyQueueStartPenerimaanCmd(PharmacyQueueStartServiceReq req)
        : this(req.LoketKey, req.RowVersion, req.UserId)
    {
    }
}

/// <summary>
/// Command pelepasan klaim loket kembali ke status Idle pada Loket 1 (Penerimaan / Release).
/// </summary>
public record PharmacyQueueReleasePenerimaanCmd(
    string LoketKey,
    byte[] RowVersion,
    string UserId = "") : IRequest<PharmacyQueueLoketCallResponse>
{
    public PharmacyQueueReleasePenerimaanCmd(PharmacyQueueReleaseReq req)
        : this(req.LoketKey, req.RowVersion, req.UserId)
    {
    }
}

#endregion

#region Command Handlers

/// <summary>
/// Handler untuk use case pemanggilan antrean pada Loket 1 (Penerimaan).
/// Memanggil tiket berikutnya berstatus TAKEN atau tiket spesifik, meng-update sesi loket fisik,
/// serta menyiarkan audio pengumuman dan refresh worklist via SignalR.
/// </summary>
public class PharmacyQueueCallPenerimaanCmdHandler
    : IRequestHandler<PharmacyQueueCallPenerimaanCmd, PharmacyQueueLoketCallResponse>
{
    private readonly IPharmacyQueueOperationRepo _operations;
    private readonly IApotekQueueEntryRepo _queueEntries;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;
    private readonly IPharmacyLoketServicePointRepo? _loketServicePoints;

    public PharmacyQueueCallPenerimaanCmdHandler(
        IPharmacyQueueOperationRepo operations,
        IApotekQueueEntryRepo queueEntries,
        IPharmacyQueuePublisher publisher,
        IAuditRepo auditRepo,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth,
        IPharmacyLoketServicePointRepo? loketServicePoints = null)
    {
        _operations = operations;
        _queueEntries = queueEntries;
        _publisher = publisher;
        _auditRepo = auditRepo;
        _clock = clock;
        _auth = auth;
        _loketServicePoints = loketServicePoints;
    }

    public async Task<PharmacyQueueLoketCallResponse> Handle(
        PharmacyQueueCallPenerimaanCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueCallPenerimaanCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));

        var at = _clock.Now;
        var actor = string.IsNullOrWhiteSpace(request.UserId) ? "SYSTEM" : request.UserId;

        var loket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (loket.LoketType != LoketTypeEnum.PenerimaanResep)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' is not a Loket Penerimaan.");

        if (loket.IsActive && loket.ClaimState != LoketClaimStateEnum.Idle)
            throw new ApotekConcurrencyException(
                request.LoketKey,
                $"Loket '{request.LoketKey}' is currently busy serving ticket '{loket.AntrianId}:{loket.NoUrut}'. Release or complete service first.");

        string targetAntrianId;
        int targetNoUrut;
        string targetQueueLabel;

        if (!string.IsNullOrWhiteSpace(request.AntrianId) && request.NoUrut.GetValueOrDefault() > 0)
        {
            var specificTicket = _queueEntries.Get(request.AntrianId, request.NoUrut!.Value)
                .GetValueOrThrow($"Pharmacy queue entry '{request.AntrianId}:{request.NoUrut.Value}' not found.");

            if (specificTicket.ApotekPhase is not (ApotekPhaseEnum.TAKEN or ApotekPhaseEnum.PENERIMAAN or ApotekPhaseEnum.DILEWATI))
                throw new InvalidOperationException(
                    $"Ticket '{specificTicket.QueueLabel}' in phase {specificTicket.ApotekPhase} cannot be called at Loket Penerimaan.");

            targetAntrianId = specificTicket.AntrianId;
            targetNoUrut = specificTicket.NoUrut;
            targetQueueLabel = specificTicket.QueueLabel;
        }
        else
        {
            var waitingTickets = _queueEntries.ListByPhase(loket.DepoFarmasiId, ApotekPhaseEnum.TAKEN);

            var assignedServicePoints = _loketServicePoints?.ListServicePointsByLoket(request.LoketKey);
            if (assignedServicePoints is { Count: > 0 })
            {
                var allowedSet = new HashSet<string>(assignedServicePoints, StringComparer.OrdinalIgnoreCase);
                waitingTickets = waitingTickets.Where(t => allowedSet.Contains(t.ServicePointId)).ToList();
            }

            var nextTicket = waitingTickets.OrderBy(t => t.NoUrut).FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"No tickets with phase TAKEN waiting in pharmacy depot '{loket.DepoFarmasiId}'.");

            targetAntrianId = nextTicket.AntrianId;
            targetNoUrut = nextTicket.NoUrut;
            targetQueueLabel = nextTicket.QueueLabel;
        }

        var expectedVersion = request.ExpectedRowVersion ?? loket.RowVersion;
        var success = _operations.TryCallLoket(
            request.LoketKey,
            targetAntrianId,
            targetNoUrut,
            expectedVersion,
            actor,
            at);

        if (!success)
            throw new ApotekConcurrencyException(
                request.LoketKey,
                $"Failed to claim call for ticket '{targetQueueLabel}' on loket '{request.LoketKey}'. Token is stale or ticket is already claimed.");

        var auditContext = AuditLogSnapshotJson.Serialize(new
        {
            request.LoketKey,
            AntrianId = targetAntrianId,
            NoUrut = targetNoUrut,
            QueueLabel = targetQueueLabel,
            ClaimState = LoketClaimStateEnum.Called.ToString(),
            ExpectedRowVersion = expectedVersion is { Length: > 0 } ? Convert.ToBase64String(expectedVersion) : null,
            UserId = actor
        });

        _auditRepo.SaveChanges(AuditLog.Create(
            actor,
            at,
            "CALL_PENERIMAAN",
            "PharmacyQueueLoketClaim",
            $"{request.LoketKey}:{targetAntrianId}:{targetNoUrut}",
            "PanggilanLoketPenerimaan",
            auditContext));

        var updatedLoket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found after call.");

        await _publisher.PublishDisplayRefresh(updatedLoket.LoketKey, updatedLoket.AnnouncementVersion, cancellationToken);
        await _publisher.PublishWorklistRefresh(updatedLoket.DepoFarmasiId, cancellationToken);

        return new PharmacyQueueLoketCallResponse(
            updatedLoket.LoketKey,
            updatedLoket.DisplayName,
            updatedLoket.LoketType,
            updatedLoket.AntrianId,
            updatedLoket.NoUrut,
            targetQueueLabel,
            updatedLoket.ClaimState,
            updatedLoket.AnnouncementVersion,
            updatedLoket.CalledAt,
            updatedLoket.RowVersion,
            true,
            $"Nomor antrean {targetQueueLabel} berhasil dipanggil di {updatedLoket.DisplayName}.");
    }
}

/// <summary>
/// Handler untuk use case pemanggilan ulang (Recall) nomor antrean aktif pada Loket 1 (Penerimaan).
/// Menaikkan counter panggilan dan versi pengumuman audio ke layar monitor.
/// </summary>
public class PharmacyQueueRecallPenerimaanCmdHandler
    : IRequestHandler<PharmacyQueueRecallPenerimaanCmd, PharmacyQueueLoketCallResponse>
{
    private readonly IPharmacyQueueOperationRepo _operations;
    private readonly IApotekQueueEntryRepo _queueEntries;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueRecallPenerimaanCmdHandler(
        IPharmacyQueueOperationRepo operations,
        IApotekQueueEntryRepo queueEntries,
        IPharmacyQueuePublisher publisher,
        IAuditRepo auditRepo,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _operations = operations;
        _queueEntries = queueEntries;
        _publisher = publisher;
        _auditRepo = auditRepo;
        _clock = clock;
        _auth = auth;
    }

    public async Task<PharmacyQueueLoketCallResponse> Handle(
        PharmacyQueueRecallPenerimaanCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueRecallPenerimaanCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));
        Guard.Against.NullOrEmpty(request.RowVersion, nameof(request.RowVersion));

        var at = _clock.Now;
        var actor = string.IsNullOrWhiteSpace(request.UserId) ? "SYSTEM" : request.UserId;

        var loket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (loket.LoketType != LoketTypeEnum.PenerimaanResep)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' is not a Loket Penerimaan.");

        if (!loket.IsActive || loket.ClaimState != LoketClaimStateEnum.Called)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' does not have an active called ticket to recall.");

        var success = _operations.TryRecallLoket(
            request.LoketKey,
            request.RowVersion,
            actor,
            at);

        if (!success)
            throw new ApotekConcurrencyException(
                request.LoketKey,
                $"Failed to recall ticket on loket '{request.LoketKey}'. Token is stale or ticket state has changed.");

        var auditContext = AuditLogSnapshotJson.Serialize(new
        {
            request.LoketKey,
            loket.AntrianId,
            loket.NoUrut,
            ExpectedRowVersion = Convert.ToBase64String(request.RowVersion),
            UserId = actor
        });

        _auditRepo.SaveChanges(AuditLog.Create(
            actor,
            at,
            "RECALL_PENERIMAAN",
            "PharmacyQueueLoketClaim",
            $"{request.LoketKey}:{loket.AntrianId}:{loket.NoUrut}",
            "PanggilanUlangLoketPenerimaan",
            auditContext));

        var updatedLoket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found after recall.");

        var ticketOpt = _queueEntries.Get(updatedLoket.AntrianId, updatedLoket.NoUrut);
        var queueLabel = ticketOpt.HasValue ? ticketOpt.Value.QueueLabel : string.Empty;

        await _publisher.PublishDisplayRefresh(updatedLoket.LoketKey, updatedLoket.AnnouncementVersion, cancellationToken);

        return new PharmacyQueueLoketCallResponse(
            updatedLoket.LoketKey,
            updatedLoket.DisplayName,
            updatedLoket.LoketType,
            updatedLoket.AntrianId,
            updatedLoket.NoUrut,
            queueLabel,
            updatedLoket.ClaimState,
            updatedLoket.AnnouncementVersion,
            updatedLoket.CalledAt,
            updatedLoket.RowVersion,
            true,
            $"Nomor antrean {queueLabel} berhasil dipanggil ulang di {updatedLoket.DisplayName}.");
    }
}

/// <summary>
/// Handler untuk use case melewati (Skip) antrean yang tidak merespons pada Loket 1 (Penerimaan).
/// Mengubah fase tiket ke DILEWATI dan melepaskan klaim loket menjadi Idle.
/// </summary>
public class PharmacyQueueSkipPenerimaanCmdHandler
    : IRequestHandler<PharmacyQueueSkipPenerimaanCmd, PharmacyQueueLoketCallResponse>
{
    private readonly IPharmacyQueueOperationRepo _operations;
    private readonly IApotekQueueEntryRepo _queueEntries;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueSkipPenerimaanCmdHandler(
        IPharmacyQueueOperationRepo operations,
        IApotekQueueEntryRepo queueEntries,
        IPharmacyQueuePublisher publisher,
        IAuditRepo auditRepo,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _operations = operations;
        _queueEntries = queueEntries;
        _publisher = publisher;
        _auditRepo = auditRepo;
        _clock = clock;
        _auth = auth;
    }

    public async Task<PharmacyQueueLoketCallResponse> Handle(
        PharmacyQueueSkipPenerimaanCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueSkipPenerimaanCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrEmpty(request.RowVersion, nameof(request.RowVersion));

        var at = _clock.Now;
        var actor = string.IsNullOrWhiteSpace(request.UserId) ? "SYSTEM" : request.UserId;

        var loket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (loket.LoketType != LoketTypeEnum.PenerimaanResep)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' is not a Loket Penerimaan.");

        if (!loket.IsActive)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' has no active ticket to skip.");

        var ticketBeforeSkipOpt = _queueEntries.Get(loket.AntrianId, loket.NoUrut);
        var queueLabel = ticketBeforeSkipOpt.HasValue ? ticketBeforeSkipOpt.Value.QueueLabel : string.Empty;

        var success = _operations.TrySkipLoket(
            request.LoketKey,
            request.Reason,
            request.RowVersion,
            actor,
            at);

        if (!success)
            throw new ApotekConcurrencyException(
                request.LoketKey,
                $"Failed to skip ticket on loket '{request.LoketKey}'. Token is stale or ticket state has changed.");

        var auditContext = AuditLogSnapshotJson.Serialize(new
        {
            request.LoketKey,
            loket.AntrianId,
            loket.NoUrut,
            QueueLabel = queueLabel,
            Reason = request.Reason,
            ExpectedRowVersion = Convert.ToBase64String(request.RowVersion),
            UserId = actor
        });

        _auditRepo.SaveChanges(AuditLog.Create(
            actor,
            at,
            "SKIP_PENERIMAAN",
            "PharmacyQueueLoketClaim",
            $"{request.LoketKey}:{loket.AntrianId}:{loket.NoUrut}",
            request.Reason,
            auditContext));

        var updatedLoket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found after skip.");

        await _publisher.PublishWorklistRefresh(updatedLoket.DepoFarmasiId, cancellationToken);
        await _publisher.PublishDisplayRefresh(updatedLoket.LoketKey, updatedLoket.AnnouncementVersion, cancellationToken);

        return new PharmacyQueueLoketCallResponse(
            updatedLoket.LoketKey,
            updatedLoket.DisplayName,
            updatedLoket.LoketType,
            updatedLoket.AntrianId,
            updatedLoket.NoUrut,
            queueLabel,
            updatedLoket.ClaimState,
            updatedLoket.AnnouncementVersion,
            updatedLoket.CalledAt,
            updatedLoket.RowVersion,
            true,
            $"Nomor antrean {queueLabel} berhasil dilewati (Skip) dengan alasan: {request.Reason}.");
    }
}

/// <summary>
/// Handler untuk use case penandaan dimulainya pelayanan tatap muka pada Loket 1 (Penerimaan).
/// Mengubah fase tiket ke PENERIMAAN dan menyinkronkan status generik InService via ITrackerPharmacyPort.
/// </summary>
public class PharmacyQueueStartPenerimaanCmdHandler
    : IRequestHandler<PharmacyQueueStartPenerimaanCmd, PharmacyQueueLoketCallResponse>
{
    private readonly IPharmacyQueueOperationRepo _operations;
    private readonly IApotekQueueEntryRepo _queueEntries;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueStartPenerimaanCmdHandler(
        IPharmacyQueueOperationRepo operations,
        IApotekQueueEntryRepo queueEntries,
        IPharmacyQueuePublisher publisher,
        IAuditRepo auditRepo,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _operations = operations;
        _queueEntries = queueEntries;
        _publisher = publisher;
        _auditRepo = auditRepo;
        _clock = clock;
        _auth = auth;
    }

    public async Task<PharmacyQueueLoketCallResponse> Handle(
        PharmacyQueueStartPenerimaanCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueStartPenerimaanCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));
        Guard.Against.NullOrEmpty(request.RowVersion, nameof(request.RowVersion));

        var at = _clock.Now;
        var actor = string.IsNullOrWhiteSpace(request.UserId) ? "SYSTEM" : request.UserId;

        var loket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (loket.LoketType != LoketTypeEnum.PenerimaanResep)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' is not a Loket Penerimaan.");

        if (!loket.IsActive || loket.ClaimState != LoketClaimStateEnum.Called)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' must be in Called state to start service.");

        var success = _operations.TryStartServiceLoket(
            request.LoketKey,
            request.RowVersion,
            actor,
            at);

        if (!success)
            throw new ApotekConcurrencyException(
                request.LoketKey,
                $"Failed to start service on loket '{request.LoketKey}'. Token is stale or ticket state has changed.");

        var auditContext = AuditLogSnapshotJson.Serialize(new
        {
            request.LoketKey,
            loket.AntrianId,
            loket.NoUrut,
            ClaimState = LoketClaimStateEnum.InService.ToString(),
            ExpectedRowVersion = Convert.ToBase64String(request.RowVersion),
            UserId = actor
        });

        _auditRepo.SaveChanges(AuditLog.Create(
            actor,
            at,
            "START_PENERIMAAN",
            "PharmacyQueueLoketClaim",
            $"{request.LoketKey}:{loket.AntrianId}:{loket.NoUrut}",
            "MulaiPelayananPenerimaan",
            auditContext));

        var updatedLoket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found after start service.");

        var ticketOpt = _queueEntries.Get(updatedLoket.AntrianId, updatedLoket.NoUrut);
        var queueLabel = ticketOpt.HasValue ? ticketOpt.Value.QueueLabel : string.Empty;

        await _publisher.PublishWorklistRefresh(updatedLoket.DepoFarmasiId, cancellationToken);

        return new PharmacyQueueLoketCallResponse(
            updatedLoket.LoketKey,
            updatedLoket.DisplayName,
            updatedLoket.LoketType,
            updatedLoket.AntrianId,
            updatedLoket.NoUrut,
            queueLabel,
            updatedLoket.ClaimState,
            updatedLoket.AnnouncementVersion,
            updatedLoket.CalledAt,
            updatedLoket.RowVersion,
            true,
            $"Pelayanan Loket 1 untuk nomor antrean {queueLabel} telah dimulai.");
    }
}

/// <summary>
/// Handler untuk use case pelepasan klaim loket kembali ke status Idle pada Loket 1 (Penerimaan / Release).
/// Digunakan saat pasien diarahkan menunggu telaah atau selesai pelayanan di loket.
/// </summary>
public class PharmacyQueueReleasePenerimaanCmdHandler
    : IRequestHandler<PharmacyQueueReleasePenerimaanCmd, PharmacyQueueLoketCallResponse>
{
    private readonly IPharmacyQueueOperationRepo _operations;
    private readonly IApotekQueueEntryRepo _queueEntries;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueReleasePenerimaanCmdHandler(
        IPharmacyQueueOperationRepo operations,
        IApotekQueueEntryRepo queueEntries,
        IPharmacyQueuePublisher publisher,
        IAuditRepo auditRepo,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _operations = operations;
        _queueEntries = queueEntries;
        _publisher = publisher;
        _auditRepo = auditRepo;
        _clock = clock;
        _auth = auth;
    }

    public async Task<PharmacyQueueLoketCallResponse> Handle(
        PharmacyQueueReleasePenerimaanCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueReleasePenerimaanCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));
        Guard.Against.NullOrEmpty(request.RowVersion, nameof(request.RowVersion));

        var at = _clock.Now;
        var actor = string.IsNullOrWhiteSpace(request.UserId) ? "SYSTEM" : request.UserId;

        var loket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (loket.LoketType != LoketTypeEnum.PenerimaanResep)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' is not a Loket Penerimaan.");

        if (!loket.IsActive)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' is already idle / released.");

        var ticketBeforeReleaseOpt = _queueEntries.Get(loket.AntrianId, loket.NoUrut);
        var queueLabel = ticketBeforeReleaseOpt.HasValue ? ticketBeforeReleaseOpt.Value.QueueLabel : string.Empty;

        var success = _operations.TryReleaseLoket(
            request.LoketKey,
            request.RowVersion,
            actor,
            at);

        if (!success)
            throw new ApotekConcurrencyException(
                request.LoketKey,
                $"Failed to release loket '{request.LoketKey}'. Token is stale or loket state has changed.");

        var auditContext = AuditLogSnapshotJson.Serialize(new
        {
            request.LoketKey,
            loket.AntrianId,
            loket.NoUrut,
            QueueLabel = queueLabel,
            ClaimState = LoketClaimStateEnum.Idle.ToString(),
            ExpectedRowVersion = Convert.ToBase64String(request.RowVersion),
            UserId = actor
        });

        _auditRepo.SaveChanges(AuditLog.Create(
            actor,
            at,
            "RELEASE_PENERIMAAN",
            "PharmacyQueueLoketClaim",
            $"{request.LoketKey}:{loket.AntrianId}:{loket.NoUrut}",
            "PelepasanKlaimLoketPenerimaan",
            auditContext));

        var updatedLoket = _operations.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found after release.");

        await _publisher.PublishWorklistRefresh(updatedLoket.DepoFarmasiId, cancellationToken);
        await _publisher.PublishDisplayRefresh(updatedLoket.LoketKey, updatedLoket.AnnouncementVersion, cancellationToken);

        return new PharmacyQueueLoketCallResponse(
            updatedLoket.LoketKey,
            updatedLoket.DisplayName,
            updatedLoket.LoketType,
            updatedLoket.AntrianId,
            updatedLoket.NoUrut,
            queueLabel,
            updatedLoket.ClaimState,
            updatedLoket.AnnouncementVersion,
            updatedLoket.CalledAt,
            updatedLoket.RowVersion,
            true,
            $"Klaim {updatedLoket.DisplayName} berhasil dilepaskan menjadi Idle.");
    }
}

#endregion
