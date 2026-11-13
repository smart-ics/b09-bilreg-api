using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

#region Command Definitions

/// <summary>
/// Command pemanggilan nomor antrean di Loket 2 (Serah Obat).
/// Dapat memanggil nomor berikutnya dalam fase SIAP_SERAH atau nomor tiket spesifik.
/// </summary>
public record PharmacyQueueCallSerahCmd(
    string LoketKey,
    string? AntrianId = null,
    int? NoUrut = null,
    byte[]? ExpectedRowVersion = null,
    string UserId = "") : IRequest<PharmacyQueueLoketCallResponse>
{
    public PharmacyQueueCallSerahCmd(PharmacyQueueCallReq req)
        : this(req.LoketKey, req.AntrianId, req.NoUrut, req.ExpectedRowVersion, req.UserId)
    {
    }
}

/// <summary>
/// Command pemanggilan ulang (Recall) nomor antrean yang sedang aktif di Loket 2 (Serah Obat).
/// </summary>
public record PharmacyQueueRecallSerahCmd(
    string LoketKey,
    byte[] RowVersion,
    string UserId = "") : IRequest<PharmacyQueueLoketCallResponse>
{
    public PharmacyQueueRecallSerahCmd(PharmacyQueueRecallReq req)
        : this(req.LoketKey, req.RowVersion, req.UserId)
    {
    }
}

/// <summary>
/// Command menandai pasien tidak merespons (Skip) di Loket 2 (Serah Obat) dan melepaskan klaim loket.
/// </summary>
public record PharmacyQueueSkipSerahCmd(
    string LoketKey,
    string Reason,
    byte[] RowVersion,
    string UserId = "") : IRequest<PharmacyQueueLoketCallResponse>
{
    public PharmacyQueueSkipSerahCmd(PharmacyQueueSkipReq req)
        : this(req.LoketKey, req.Reason, req.RowVersion, req.UserId)
    {
    }
}

/// <summary>
/// Command penandaan dimulainya tatap muka penyerahan obat fisik di Loket 2 (SERAH_OBAT).
/// </summary>
public record PharmacyQueueStartSerahCmd(
    string LoketKey,
    byte[] RowVersion,
    string UserId = "") : IRequest<PharmacyQueueLoketCallResponse>
{
    public PharmacyQueueStartSerahCmd(PharmacyQueueStartServiceReq req)
        : this(req.LoketKey, req.RowVersion, req.UserId)
    {
    }
}

#endregion

#region Command Handlers

public class PharmacyQueueCallSerahHandler : IRequestHandler<PharmacyQueueCallSerahCmd, PharmacyQueueLoketCallResponse>
{
    private readonly IPharmacyQueueOperationRepo _operationRepo;
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueCallSerahHandler(
        IPharmacyQueueOperationRepo operationRepo,
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyQueuePublisher publisher,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _operationRepo = operationRepo;
        _queueEntryRepo = queueEntryRepo;
        _publisher = publisher;
        _clock = clock;
        _auth = auth;
    }

    public async Task<PharmacyQueueLoketCallResponse> Handle(
        PharmacyQueueCallSerahCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueCallSerahCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));

        var loket = _operationRepo.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (loket.LoketType != LoketTypeEnum.SerahObat)
            throw new ApotekDomainException($"Loket '{request.LoketKey}' is not configured for Serah Obat (LoketType={loket.LoketType}).");

        string antrianId;
        int noUrut;

        if (!string.IsNullOrWhiteSpace(request.AntrianId) && request.NoUrut is > 0)
        {
            antrianId = request.AntrianId.Trim();
            noUrut = request.NoUrut.Value;

            var entry = _queueEntryRepo.Get(antrianId, noUrut)
                .GetValueOrThrow($"Queue entry '{antrianId}:{noUrut}' not found.");

            if (entry.ApotekPhase is not (ApotekPhaseEnum.SIAP_SERAH or ApotekPhaseEnum.SERAH_OBAT or ApotekPhaseEnum.DILEWATI))
                throw new ApotekDomainException($"Cannot call at Loket Serah when phase is '{entry.ApotekPhase}'.");
        }
        else
        {
            var candidates = _queueEntryRepo.ListByPhase(loket.DepoFarmasiId, ApotekPhaseEnum.SIAP_SERAH);
            if (candidates.Count == 0)
                throw new InvalidOperationException("Tidak ada antrean berstatus SIAP_SERAH yang dapat dipanggil.");

            var next = candidates.OrderBy(x => x.PreparedAt).ThenBy(x => x.NoUrut).First();
            antrianId = next.AntrianId;
            noUrut = next.NoUrut;
        }

        var now = _clock.Now;
        var expectedRv = request.ExpectedRowVersion ?? loket.RowVersion;

        var success = _operationRepo.TryCallLoket(
            request.LoketKey,
            antrianId,
            noUrut,
            expectedRv,
            request.UserId,
            now);

        if (!success)
            throw new ApotekConcurrencyException(request.LoketKey, 0);

        var updatedLoket = _operationRepo.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' reload failed.");
        var updatedEntry = _queueEntryRepo.Get(antrianId, noUrut)
            .GetValueOrThrow($"Queue entry '{antrianId}:{noUrut}' reload failed.");

        await _publisher.PublishDisplayRefresh(updatedLoket.LoketKey, updatedLoket.AnnouncementVersion, cancellationToken);
        await _publisher.PublishWorklistRefresh(updatedLoket.DepoFarmasiId, cancellationToken);

        return new PharmacyQueueLoketCallResponse(
            LoketKey: updatedLoket.LoketKey,
            DisplayName: updatedLoket.DisplayName,
            LoketType: updatedLoket.LoketType,
            AntrianId: updatedLoket.AntrianId,
            NoUrut: updatedLoket.NoUrut,
            QueueLabel: updatedEntry.QueueLabel,
            ClaimState: updatedLoket.ClaimState,
            AnnouncementVersion: updatedLoket.AnnouncementVersion,
            CalledAt: updatedLoket.CalledAt,
            RowVersion: updatedLoket.RowVersion,
            Success: true,
            Message: "Nomor antrean berhasil dipanggil ke Loket Serah Obat.");
    }
}

public class PharmacyQueueRecallSerahHandler : IRequestHandler<PharmacyQueueRecallSerahCmd, PharmacyQueueLoketCallResponse>
{
    private readonly IPharmacyQueueOperationRepo _operationRepo;
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueRecallSerahHandler(
        IPharmacyQueueOperationRepo operationRepo,
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyQueuePublisher publisher,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _operationRepo = operationRepo;
        _queueEntryRepo = queueEntryRepo;
        _publisher = publisher;
        _clock = clock;
        _auth = auth;
    }

    public async Task<PharmacyQueueLoketCallResponse> Handle(
        PharmacyQueueRecallSerahCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueRecallSerahCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));

        var loket = _operationRepo.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (loket.LoketType != LoketTypeEnum.SerahObat)
            throw new ApotekDomainException($"Loket '{request.LoketKey}' is not configured for Serah Obat.");

        if (loket.ClaimState == LoketClaimStateEnum.Idle || string.IsNullOrWhiteSpace(loket.AntrianId) || loket.NoUrut <= 0)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' does not have an active ticket to recall.");

        var now = _clock.Now;
        var success = _operationRepo.TryRecallLoket(
            request.LoketKey,
            request.RowVersion,
            request.UserId,
            now);

        if (!success)
            throw new ApotekConcurrencyException(request.LoketKey, 0);

        var updatedLoket = _operationRepo.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' reload failed.");
        var entry = _queueEntryRepo.Get(updatedLoket.AntrianId, updatedLoket.NoUrut);
        var label = entry.HasValue ? entry.Value.QueueLabel : "";

        await _publisher.PublishDisplayRefresh(updatedLoket.LoketKey, updatedLoket.AnnouncementVersion, cancellationToken);

        return new PharmacyQueueLoketCallResponse(
            LoketKey: updatedLoket.LoketKey,
            DisplayName: updatedLoket.DisplayName,
            LoketType: updatedLoket.LoketType,
            AntrianId: updatedLoket.AntrianId,
            NoUrut: updatedLoket.NoUrut,
            QueueLabel: label,
            ClaimState: updatedLoket.ClaimState,
            AnnouncementVersion: updatedLoket.AnnouncementVersion,
            CalledAt: updatedLoket.CalledAt,
            RowVersion: updatedLoket.RowVersion,
            Success: true,
            Message: "Panggilan ulang berhasil disiarkan.");
    }
}

public class PharmacyQueueSkipSerahHandler : IRequestHandler<PharmacyQueueSkipSerahCmd, PharmacyQueueLoketCallResponse>
{
    private readonly IPharmacyQueueOperationRepo _operationRepo;
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueSkipSerahHandler(
        IPharmacyQueueOperationRepo operationRepo,
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyQueuePublisher publisher,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _operationRepo = operationRepo;
        _queueEntryRepo = queueEntryRepo;
        _publisher = publisher;
        _clock = clock;
        _auth = auth;
    }

    public async Task<PharmacyQueueLoketCallResponse> Handle(
        PharmacyQueueSkipSerahCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueSkipSerahCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var loket = _operationRepo.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (loket.LoketType != LoketTypeEnum.SerahObat)
            throw new ApotekDomainException($"Loket '{request.LoketKey}' is not configured for Serah Obat.");

        if (loket.ClaimState == LoketClaimStateEnum.Idle || string.IsNullOrWhiteSpace(loket.AntrianId) || loket.NoUrut <= 0)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' does not have an active ticket to skip.");

        var now = _clock.Now;
        var skippedAntrianId = loket.AntrianId;
        var skippedNoUrut = loket.NoUrut;

        var success = _operationRepo.TrySkipLoket(
            request.LoketKey,
            request.Reason,
            request.RowVersion,
            request.UserId,
            now);

        if (!success)
            throw new ApotekConcurrencyException(request.LoketKey, 0);

        var updatedLoket = _operationRepo.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' reload failed.");
        var entry = _queueEntryRepo.Get(skippedAntrianId, skippedNoUrut);
        var label = entry.HasValue ? entry.Value.QueueLabel : "";

        await _publisher.PublishWorklistRefresh(loket.DepoFarmasiId, cancellationToken);

        return new PharmacyQueueLoketCallResponse(
            LoketKey: updatedLoket.LoketKey,
            DisplayName: updatedLoket.DisplayName,
            LoketType: updatedLoket.LoketType,
            AntrianId: skippedAntrianId,
            NoUrut: skippedNoUrut,
            QueueLabel: label,
            ClaimState: updatedLoket.ClaimState,
            AnnouncementVersion: updatedLoket.AnnouncementVersion,
            CalledAt: updatedLoket.CalledAt,
            RowVersion: updatedLoket.RowVersion,
            Success: true,
            Message: "Nomor antrean dilewati dan klaim loket dilepaskan.");
    }
}

public class PharmacyQueueStartSerahHandler : IRequestHandler<PharmacyQueueStartSerahCmd, PharmacyQueueLoketCallResponse>
{
    private readonly IPharmacyQueueOperationRepo _operationRepo;
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueStartSerahHandler(
        IPharmacyQueueOperationRepo operationRepo,
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyQueuePublisher publisher,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth)
    {
        _operationRepo = operationRepo;
        _queueEntryRepo = queueEntryRepo;
        _publisher = publisher;
        _clock = clock;
        _auth = auth;
    }

    public async Task<PharmacyQueueLoketCallResponse> Handle(
        PharmacyQueueStartSerahCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueStartSerahCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.LoketKey, nameof(request.LoketKey));

        var loket = _operationRepo.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' not found.");

        if (loket.LoketType != LoketTypeEnum.SerahObat)
            throw new ApotekDomainException($"Loket '{request.LoketKey}' is not configured for Serah Obat.");

        if (loket.ClaimState != LoketClaimStateEnum.Called)
            throw new InvalidOperationException($"Loket '{request.LoketKey}' must be in Called state to start service.");

        var now = _clock.Now;
        var success = _operationRepo.TryStartServiceLoket(
            request.LoketKey,
            request.RowVersion,
            request.UserId,
            now);

        if (!success)
            throw new ApotekConcurrencyException(request.LoketKey, 0);

        var updatedLoket = _operationRepo.GetLoket(request.LoketKey)
            .GetValueOrThrow($"Loket '{request.LoketKey}' reload failed.");
        var entry = _queueEntryRepo.Get(updatedLoket.AntrianId, updatedLoket.NoUrut)
            .GetValueOrThrow($"Queue entry '{updatedLoket.AntrianId}:{updatedLoket.NoUrut}' not found.");

        await _publisher.PublishWorklistRefresh(updatedLoket.DepoFarmasiId, cancellationToken);

        return new PharmacyQueueLoketCallResponse(
            LoketKey: updatedLoket.LoketKey,
            DisplayName: updatedLoket.DisplayName,
            LoketType: updatedLoket.LoketType,
            AntrianId: updatedLoket.AntrianId,
            NoUrut: updatedLoket.NoUrut,
            QueueLabel: entry.QueueLabel,
            ClaimState: updatedLoket.ClaimState,
            AnnouncementVersion: updatedLoket.AnnouncementVersion,
            CalledAt: updatedLoket.CalledAt,
            RowVersion: updatedLoket.RowVersion,
            Success: true,
            Message: "Pelayanan penyerahan obat di Loket 2 dimulai.");
    }
}

#endregion
