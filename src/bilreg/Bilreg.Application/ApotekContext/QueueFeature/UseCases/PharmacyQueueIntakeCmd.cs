using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

/// <summary>
/// Command intake tiket antrean farmasi dari mesin kiosk mandiri.
/// Mendukung alur scan nomor registrasi (RegId) dengan klasifikasi otomatis BPJS/Umum,
/// serta fallback antrean umum mandiri.
/// </summary>
public record PharmacyQueueIntakeCmd(
    string ServicePointId = "",
    string DepoFarmasiId = "",
    string RegId = "",
    string UserId = "KIOSK") : IRequest<PharmacyQueueIntakeResponse>
{
    public PharmacyQueueIntakeCmd(PharmacyQueueIntakeReq req)
        : this(
            ServicePointId: req.ServicePointId ?? "",
            DepoFarmasiId: req.DepoFarmasiId ?? "",
            RegId: req.RegId ?? "",
            UserId: string.IsNullOrWhiteSpace(req.UserId) ? "KIOSK" : req.UserId)
    {
    }
}

/// <summary>
/// Handler untuk use case alokasi on-demand tiket antrean farmasi di kiosk mandiri.
/// Mengalokasikan nomor antrean sekuensial on-demand via ISequencer tanpa membuat baris OPEN,
/// mengevaluasi tipe jaminan pasien (BPJS prefix A vs Umum prefix B),
/// melakukan auto demand mapping untuk e-resep aktif milik registrasi ke BILRG_AptQueueMapping,
/// membuat entri generik BILRG_AntrianEntry (Waiting = 0) dan entri apotek BILRG_AptQueueEntry (ApotekPhase = TAKEN),
/// serta menyiarkan notifikasi SignalR ke worklist Loket 1.
/// </summary>
public class PharmacyQueueIntakeHandler : IRequestHandler<PharmacyQueueIntakeCmd, PharmacyQueueIntakeResponse>
{
    public const string ServicePointBpjsId = "SP-FAR-BPJS";
    public const string ServicePointUmumId = "SP-FAR-UMUM";

    private readonly IPharmacyServicePointRepo _servicePoints;
    private readonly IApotekQueueEntryRepo _queueEntries;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly ISequencer _sequencer;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly ITglJamProvider _clock;
    private readonly IAptAuthorizationPolicy _auth;
    private readonly IRegRepo? _regRepo;
    private readonly IResepRepo? _resepRepo;
    private readonly IPharmacyQueueMappingRepo? _mappingRepo;

    public PharmacyQueueIntakeHandler(
        IPharmacyServicePointRepo servicePoints,
        IApotekQueueEntryRepo queueEntries,
        IAntrianRepo antrianRepo,
        IAntrianFactory antrianFactory,
        ISequencer sequencer,
        IPharmacyQueuePublisher publisher,
        ITglJamProvider clock,
        IAptAuthorizationPolicy auth,
        IRegRepo? regRepo = null,
        IResepRepo? resepRepo = null,
        IPharmacyQueueMappingRepo? mappingRepo = null)
    {
        _servicePoints = servicePoints;
        _queueEntries = queueEntries;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _sequencer = sequencer;
        _publisher = publisher;
        _clock = clock;
        _auth = auth;
        _regRepo = regRepo;
        _resepRepo = resepRepo;
        _mappingRepo = mappingRepo;
    }

    public async Task<PharmacyQueueIntakeResponse> Handle(
        PharmacyQueueIntakeCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueIntakeCmd), request.UserId);

        RegModel? reg = null;
        string targetServicePointId;
        List<ResepModel> activeReseps = [];

        if (!string.IsNullOrWhiteSpace(request.RegId))
        {
            var regKey = RegModel.Key(request.RegId.Trim());
            reg = _regRepo.LoadEntity(regKey)
                .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");

            targetServicePointId = IsBpjs(reg) ? ServicePointBpjsId : ServicePointUmumId;

            activeReseps = (_resepRepo.ListData(regKey) ?? [])
                .Where(x => !x.IsVoid)
                .ToList();
        }
        else
        {
            targetServicePointId = !string.IsNullOrWhiteSpace(request.ServicePointId)
                ? request.ServicePointId.Trim()
                : ServicePointUmumId;
        }

        var servicePoint = _servicePoints.Get(targetServicePointId)
            .GetValueOrThrow($"Pharmacy service point '{targetServicePointId}' not found");

        if (!servicePoint.Active)
            throw new InvalidOperationException($"Pharmacy service point '{servicePoint.ServicePointId}' is not active.");

        var occurredAt = _clock.Now;
        var businessDate = DateOnly.FromDateTime(occurredAt);

        var reference = new ServicePointType(servicePoint.ServicePointId, servicePoint.DisplayName);
        var sequenceTag = AntrianModel.GenSequenceTag(businessDate, TimeOnly.MinValue, reference);

        var queue = ResolveOrCreateQueue(servicePoint, businessDate, sequenceTag);

        PharmacyQueueIntakeResponse response;
        try
        {
            response = CommitIntake(queue, servicePoint, sequenceTag, occurredAt, reg, activeReseps, request.UserId);
        }
        catch (AdmissionQueueSessionRaceException)
        {
            // One-winner reload jika terjadi balapan pembentukan header sesi antrean
            var winner = ReloadWinnerSession(businessDate, sequenceTag);
            response = CommitIntake(winner, servicePoint, sequenceTag, occurredAt, reg, activeReseps, request.UserId);
        }

        var broadcastDepo = !string.IsNullOrWhiteSpace(servicePoint.DepoFarmasiId)
            ? servicePoint.DepoFarmasiId
            : request.DepoFarmasiId;

        await _publisher.PublishWorklistRefresh(broadcastDepo, cancellationToken);
        return response;
    }

    private static bool IsBpjs(RegModel reg)
    {
        if (reg.TipeJaminan is null)
            return false;

        var id = reg.TipeJaminan.TipeJaminanId?.Trim() ?? string.Empty;
        var name = reg.TipeJaminan.TipeJaminanName?.Trim() ?? string.Empty;

        if (id == "1" || id.Contains("BPJS", StringComparison.OrdinalIgnoreCase))
            return true;

        if (name.Contains("BPJS", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private AntrianModel ResolveOrCreateQueue(
        PharmacyServicePointModel servicePoint,
        DateOnly businessDate,
        string sequenceTag)
    {
        var existingView = _antrianRepo.ListData(businessDate)
            .FirstOrDefault(x => x.SequenceTag == sequenceTag);

        if (existingView is not null)
            return _antrianRepo.LoadEntity(existingView).Value;

        var reference = new ServicePointType(servicePoint.ServicePointId, servicePoint.DisplayName);
        return _antrianFactory.Create(reference, businessDate);
    }

    private AntrianModel ReloadWinnerSession(DateOnly businessDate, string sequenceTag)
    {
        var winnerView = _antrianRepo.ListData(businessDate)
            .FirstOrDefault(x => x.SequenceTag == sequenceTag)
            ?? throw new AdmissionQueueConcurrencyException(
                $"Daily queue session for '{sequenceTag}' was created concurrently but could not be reloaded.");

        return _antrianRepo.LoadEntity(winnerView)
            .GetValueOrThrow($"Admission queue session '{winnerView.AntrianId}' not found after concurrent create.");
    }

    private PharmacyQueueIntakeResponse CommitIntake(
        AntrianModel queue,
        PharmacyServicePointModel servicePoint,
        string sequenceTag,
        DateTime occurredAt,
        RegModel? reg,
        IReadOnlyList<ResepModel> activeReseps,
        string userId)
    {
        var genericEntry = queue.AddEntry(occurredAt);

        var aptEntry = ApotekQueueEntryModel.Create(
            queue.AntrianId,
            genericEntry.NoUrut,
            servicePoint.ServicePointId,
            servicePoint.QueuePrefix,
            occurredAt);

        using var trans = TransHelper.NewScope();
        _antrianRepo.SaveNewEntry(queue, genericEntry);
        _queueEntries.Save(aptEntry);

        foreach (var resep in activeReseps)
        {
            var existingOpt = _mappingRepo.GetByDemand(QueueDemandKindEnum.ResepKerja, resep.ResepId);
            QueueMappingModel mapping;
            if (existingOpt.HasValue)
            {
                mapping = existingOpt.Value;
                mapping.Correct(
                    queue.AntrianId,
                    aptEntry.NoUrut,
                    mapping.PasienTrackerId,
                    userId,
                    occurredAt);
            }
            else
            {
                mapping = QueueMappingModel.Create(
                    QueueDemandKindEnum.ResepKerja,
                    resep.ResepId,
                    queue.AntrianId,
                    aptEntry.NoUrut,
                    pasienTrackerId: string.Empty,
                    QueueMappingMethodEnum.Tracker,
                    mappedBy: userId,
                    mappedAt: occurredAt);
            }

            _mappingRepo.Save(mapping);
        }

        trans.Complete();

        var waitingAheadCount = _queueEntries.ListByPhase(servicePoint.DepoFarmasiId, ApotekPhaseEnum.TAKEN)
            .Count(x => x.ServicePointId == servicePoint.ServicePointId && x.NoUrut < aptEntry.NoUrut);

        return new PharmacyQueueIntakeResponse(
            AntrianId: queue.AntrianId,
            NoUrut: aptEntry.NoUrut,
            QueueLabel: aptEntry.QueueLabel,
            ServicePointId: servicePoint.ServicePointId,
            ServicePointName: servicePoint.DisplayName,
            QueuePrefix: servicePoint.QueuePrefix,
            ApotekPhase: aptEntry.ApotekPhase,
            TakenAt: aptEntry.TakenAt,
            WaitingAheadCount: waitingAheadCount,
            PatientName: reg?.Pasien?.PasienName ?? string.Empty,
            MedicalRecordNo: reg?.Pasien?.PasienId ?? string.Empty,
            Penjamin: reg?.TipeJaminan?.TipeJaminanName ?? string.Empty);
    }
}
