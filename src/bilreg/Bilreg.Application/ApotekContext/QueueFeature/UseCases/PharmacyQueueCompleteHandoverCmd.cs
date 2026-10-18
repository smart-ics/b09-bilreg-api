using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

/// <summary>
/// Command penyelesaian penyerahan fisik obat di Loket 2 (Serah Obat) (TD-09).
/// Memvalidasi gating kasir / keabsahan SEP BPJS, mendukung override darurat klinis oleh Apoteker,
/// mentransisikan fase antrean ke terminal SELESAI, dan menyinkronkan status generik ke Done.
/// </summary>
public record PharmacyQueueCompleteHandoverCmd(
    string AntrianId,
    int NoUrut,
    string PenjualanId = "",
    string NoSep = "",
    bool IsPaymentOverridden = false,
    string PaymentOverrideReason = "",
    string PaymentOverridePharmacistId = "",
    string UserId = "",
    string? LoketKey = null,
    byte[]? ExpectedLoketRowVersion = null) : IRequest<PharmacyQueueCompleteHandoverResponse>
{
    public PharmacyQueueCompleteHandoverCmd(PharmacyQueueCompleteHandoverReq req)
        : this(
            req.AntrianId,
            req.NoUrut,
            req.PenjualanId,
            req.NoSep,
            req.IsPaymentOverridden,
            req.PaymentOverrideReason,
            req.PaymentOverridePharmacistId,
            req.UserId)
    {
    }
}

public class PharmacyQueueCompleteHandoverHandler
    : IRequestHandler<PharmacyQueueCompleteHandoverCmd, PharmacyQueueCompleteHandoverResponse>
{
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyServicePointRepo _servicePointRepo;
    private readonly IPharmacyQueueMappingRepo _mappingRepo;
    private readonly IPaymentClearancePort _paymentClearancePort;
    private readonly IPharmacyQueueOperationRepo _operationRepo;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;
    private readonly ITrackerPharmacyPort? _tracker;

    public PharmacyQueueCompleteHandoverHandler(
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyServicePointRepo servicePointRepo,
        IPharmacyQueueMappingRepo mappingRepo,
        IPaymentClearancePort paymentClearancePort,
        IPharmacyQueueOperationRepo operationRepo,
        IPharmacyQueuePublisher publisher,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth,
        ITrackerPharmacyPort? tracker = null)
    {
        _queueEntryRepo = queueEntryRepo;
        _servicePointRepo = servicePointRepo;
        _mappingRepo = mappingRepo;
        _paymentClearancePort = paymentClearancePort;
        _operationRepo = operationRepo;
        _publisher = publisher;
        _clock = clock;
        _auth = auth;
        _tracker = tracker;
    }

    public async Task<PharmacyQueueCompleteHandoverResponse> Handle(
        PharmacyQueueCompleteHandoverCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueCompleteHandoverCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.AntrianId, nameof(request.AntrianId));
        Guard.Against.NegativeOrZero(request.NoUrut, nameof(request.NoUrut));

        var entry = _queueEntryRepo.Get(request.AntrianId, request.NoUrut)
            .GetValueOrThrow($"Queue entry '{request.AntrianId}:{request.NoUrut}' not found.");

        if (entry.ApotekPhase is not (ApotekPhaseEnum.SERAH_OBAT or ApotekPhaseEnum.SIAP_SERAH))
            throw new ApotekDomainException($"Cannot complete handover when phase is '{entry.ApotekPhase}'. Handover requires SIAP_SERAH or SERAH_OBAT.");

        // 1. Evaluasi Gating Pelunasan Kasir / SEP (TD-09)
        if (request.IsPaymentOverridden)
        {
            if (string.IsNullOrWhiteSpace(request.PaymentOverrideReason))
                throw new ApotekDomainException("Payment override reason is required when payment is overridden.");
            if (string.IsNullOrWhiteSpace(request.PaymentOverridePharmacistId))
                throw new ApotekDomainException("Pharmacist ID is required when payment is overridden.");
        }
        else
        {
            var checkResult = _paymentClearancePort.CheckClearance(request.PenjualanId, request.NoSep);
            if (!checkResult.IsCleared)
            {
                throw new ApotekDomainException(
                    $"Penyerahan obat diblokir karena gating kasir/SEP belum terpenuhi: {checkResult.Message}. Gunakan override Apoteker untuk pengecualian klinis.");
            }
        }

        // 2. Transisi Agregat Domain ke SELESAI
        var now = _clock.Now;
        entry.CompleteHandover(
            now,
            request.IsPaymentOverridden,
            request.PaymentOverrideReason,
            request.PaymentOverridePharmacistId);

        // 3. Persistensi Atomik & Sinkronisasi Dual-Layer (ADR-APT-001)
        using (var trans = TransHelper.NewScope())
        {
            _queueEntryRepo.Save(entry);

            if (_tracker != null)
            {
                var mappings = _mappingRepo.ListByQueue(entry.AntrianId, entry.NoUrut) ?? [];
                var pasienTrackerId = mappings.FirstOrDefault()?.PasienTrackerId ?? "";
                _tracker.DoneOnce(entry.AntrianId, entry.NoUrut, pasienTrackerId, entry.QueueLabel, now);
            }

            // Lepas sesi klaim loket jika ada loket yang sedang memegang tiket ini
            if (!string.IsNullOrWhiteSpace(request.LoketKey))
            {
                var loketOpt = _operationRepo.GetLoket(request.LoketKey);
                if (loketOpt.HasValue && loketOpt.Value.IsActive &&
                    loketOpt.Value.AntrianId == entry.AntrianId && loketOpt.Value.NoUrut == entry.NoUrut)
                {
                    _operationRepo.TryReleaseLoket(
                        request.LoketKey,
                        request.ExpectedLoketRowVersion ?? loketOpt.Value.RowVersion,
                        request.UserId,
                        now);
                }
            }
            else
            {
                var spOpt = _servicePointRepo.Get(entry.ServicePointId);
                if (spOpt.HasValue)
                {
                    var activeLokets = _operationRepo.ListLoketByDepo(spOpt.Value.DepoFarmasiId) ?? [];
                    var holdingLoket = activeLokets.FirstOrDefault(l =>
                        l.IsActive && l.AntrianId == entry.AntrianId && l.NoUrut == entry.NoUrut);
                    if (holdingLoket != null)
                    {
                        _operationRepo.TryReleaseLoket(
                            holdingLoket.LoketKey,
                            holdingLoket.RowVersion,
                            request.UserId,
                            now);
                    }
                }
            }

            trans.Complete();
        }

        // 4. Siaran Real-Time Refresh
        var sp = _servicePointRepo.Get(entry.ServicePointId);
        var depoId = sp.HasValue ? sp.Value.DepoFarmasiId : "";
        await _publisher.PublishWorklistRefresh(depoId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.LoketKey))
        {
            var loket = _operationRepo.GetLoket(request.LoketKey);
            if (loket.HasValue)
            {
                await _publisher.PublishDisplayRefresh(loket.Value.LoketKey, loket.Value.AnnouncementVersion, cancellationToken);
            }
        }

        return new PharmacyQueueCompleteHandoverResponse(
            AntrianId: entry.AntrianId,
            NoUrut: entry.NoUrut,
            ApotekPhase: entry.ApotekPhase,
            DoneAt: entry.DoneAt,
            IsPaymentOverridden: entry.IsPaymentOverridden,
            Success: true,
            Message: entry.IsPaymentOverridden
                ? "Penyerahan obat selesai dengan override klinis Apoteker."
                : "Penyerahan obat selesai dan tagihan/SEP terverifikasi valid.");
    }
}
