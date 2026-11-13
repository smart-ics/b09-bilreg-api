using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

#region Command Records

/// <summary>
/// Command untuk menautkan tiket antrean fisik ke satu atau beberapa e-resep aktif pasien (BILRG_AptQueueMapping) (TD-05).
/// </summary>
public record PharmacyQueueMapDemandCmd(
    string AntrianId,
    int NoUrut,
    QueueDemandKindEnum DemandKind,
    string DemandId,
    IReadOnlyList<string>? DemandIds = null,
    string PasienTrackerId = "",
    QueueMappingMethodEnum MappingMethod = QueueMappingMethodEnum.Manual,
    string UserId = ""
) : IRequest<PharmacyQueueMapDemandResponse>
{
    public PharmacyQueueMapDemandCmd(PharmacyQueueMapDemandReq req)
        : this(
            req.AntrianId,
            req.NoUrut,
            req.DemandKind,
            req.DemandId,
            null,
            req.PasienTrackerId,
            req.MappingMethod,
            req.UserId)
    {
    }
}

#endregion

#region Command Handler

/// <summary>
/// Handler pemetaan tiket antrean ke e-resep aktif pasien pada BILRG_AptQueueMapping.
/// Mendukung pengaitan multi-resep ke satu nomor tiket antrean yang sama.
/// </summary>
public class PharmacyQueueMapDemandHandler : IRequestHandler<PharmacyQueueMapDemandCmd, PharmacyQueueMapDemandResponse>
{
    private readonly IPharmacyQueueMappingRepo _mappingRepo;
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyServicePointRepo _servicePointRepo;
    private readonly IResepRepo _resepRepo;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueMapDemandHandler(
        IPharmacyQueueMappingRepo mappingRepo,
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyServicePointRepo servicePointRepo,
        IResepRepo resepRepo,
        IPharmacyQueuePublisher publisher,
        IAptAuthorizationPolicy auth)
    {
        _mappingRepo = mappingRepo;
        _queueEntryRepo = queueEntryRepo;
        _servicePointRepo = servicePointRepo;
        _resepRepo = resepRepo;
        _publisher = publisher;
        _auth = auth;
    }

    public async Task<PharmacyQueueMapDemandResponse> Handle(
        PharmacyQueueMapDemandCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueMapDemandCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.AntrianId, nameof(request.AntrianId));
        Guard.Against.NegativeOrZero(request.NoUrut, nameof(request.NoUrut));

        // 1. Validasi keberadaan entri antrean farmasi
        var entryOpt = _queueEntryRepo.Get(request.AntrianId, request.NoUrut);
        if (!entryOpt.HasValue)
            throw new ApotekDomainException($"Antrean '{request.AntrianId}-{request.NoUrut}' tidak ditemukan.");

        // 2. Kumpulkan seluruh demand ID yang hendak dipetakan
        var demandIds = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.DemandId))
            demandIds.Add(request.DemandId.Trim());

        if (request.DemandIds != null)
        {
            foreach (var id in request.DemandIds)
            {
                if (!string.IsNullOrWhiteSpace(id) && !demandIds.Contains(id.Trim(), StringComparer.OrdinalIgnoreCase))
                    demandIds.Add(id.Trim());
            }
        }

        if (demandIds.Count == 0)
            throw new ApotekDomainException("Minimal satu nomor dokumen permintaan harus ditentukan.");

        // 3. Validasi keberadaan resep jika demand kind adalah Resep / ResepKerja
        foreach (var id in demandIds)
        {
            if (request.DemandKind is QueueDemandKindEnum.Resep or QueueDemandKindEnum.ResepKerja)
            {
                _resepRepo.LoadEntity(ResepModel.Key(id))
                    .GetValueOrThrow($"Resep '{id}' tidak ditemukan.");
            }
        }

        // 4. Petakan tiket antrean ke seluruh e-resep di dalam transaksi
        var now = DateTime.Now;
        using var trans = TransHelper.NewScope();

        foreach (var id in demandIds)
        {
            var existingOpt = _mappingRepo.GetByDemand(request.DemandKind, id);
            QueueMappingModel model;
            if (existingOpt.HasValue)
            {
                model = existingOpt.Value;
                model.Correct(
                    request.AntrianId,
                    request.NoUrut,
                    request.PasienTrackerId,
                    request.UserId,
                    now);
            }
            else
            {
                model = QueueMappingModel.Create(
                    request.DemandKind,
                    id,
                    request.AntrianId,
                    request.NoUrut,
                    request.PasienTrackerId,
                    request.MappingMethod,
                    request.UserId,
                    now);
            }

            _mappingRepo.Save(model);
        }

        trans.Complete();

        // 5. Siarkan hint penyegaran worklist ke depo SignalR
        var entry = entryOpt.Value;
        var spOpt = _servicePointRepo.Get(entry.ServicePointId);
        var depoId = spOpt.HasValue ? spOpt.Value.DepoFarmasiId : string.Empty;
        await _publisher.PublishWorklistRefresh(depoId, cancellationToken);

        return new PharmacyQueueMapDemandResponse(
            request.AntrianId,
            request.NoUrut,
            request.DemandKind,
            demandIds.First(),
            now,
            true);
    }
}

#endregion
