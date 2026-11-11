using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

#region Command Records

/// <summary>
/// Command penandaan kehadiran fisik pasien di depan loket penerimaan (TD-05).
/// Menandai PasienHadir = true, PasienHadirAt = now pada kaitan tiket antrean,
/// dan menyiarkan notifikasi prioritas ke worklist Telaah Resep via SignalR.
/// </summary>
public record PharmacyQueueFlagPasienHadirCmd(
    QueueDemandKindEnum DemandKind,
    string DemandId,
    string AntrianId,
    int NoUrut,
    string UserId = ""
) : IRequest<PharmacyQueueFlagPasienHadirResponse>
{
    public PharmacyQueueFlagPasienHadirCmd(PharmacyQueueFlagPasienHadirReq req)
        : this(
            req.DemandKind,
            req.DemandId,
            req.AntrianId,
            req.NoUrut,
            req.UserId)
    {
    }
}

#endregion

#region Command Handler

/// <summary>
/// Handler penandaan kehadiran fisik pasien di depan loket penerimaan untuk resep yang belum ditelaah.
/// Menyiarkan sinyal prioritas elevasi ke worklist Apoteker (TelaahResep.vue) via SignalR.
/// </summary>
public class PharmacyQueueFlagPasienHadirHandler
    : IRequestHandler<PharmacyQueueFlagPasienHadirCmd, PharmacyQueueFlagPasienHadirResponse>
{
    private readonly IPharmacyQueueMappingRepo _mappingRepo;
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyServicePointRepo _servicePointRepo;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly IAptAuthorizationPolicy _auth;

    public PharmacyQueueFlagPasienHadirHandler(
        IPharmacyQueueMappingRepo mappingRepo,
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyServicePointRepo servicePointRepo,
        IPharmacyQueuePublisher publisher,
        IAptAuthorizationPolicy auth)
    {
        _mappingRepo = mappingRepo;
        _queueEntryRepo = queueEntryRepo;
        _servicePointRepo = servicePointRepo;
        _publisher = publisher;
        _auth = auth;
    }

    public async Task<PharmacyQueueFlagPasienHadirResponse> Handle(
        PharmacyQueueFlagPasienHadirCmd request,
        CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PharmacyQueueFlagPasienHadirCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.AntrianId, nameof(request.AntrianId));
        Guard.Against.NegativeOrZero(request.NoUrut, nameof(request.NoUrut));

        // 1. Validasi keberadaan antrean apotek
        var entryOpt = _queueEntryRepo.Get(request.AntrianId, request.NoUrut);
        if (!entryOpt.HasValue)
            throw new ApotekDomainException($"Antrean '{request.AntrianId}-{request.NoUrut}' tidak ditemukan.");

        var now = DateTime.Now;

        // 2. Tandai status kehadiran fisik pasien pada dokumen permintaan obat yang dipetakan
        using (var trans = TransHelper.NewScope())
        {
            if (!string.IsNullOrWhiteSpace(request.DemandId))
            {
                var mapOpt = _mappingRepo.GetByDemand(request.DemandKind, request.DemandId);
                if (mapOpt.HasValue)
                {
                    var map = mapOpt.Value;
                    map.MarkPasienHadir(now);
                    _mappingRepo.Save(map);
                }
                else
                {
                    _mappingRepo.SetPasienHadir(request.DemandKind, request.DemandId, request.UserId, now);
                }
            }

            var queueMappings = _mappingRepo.ListByQueue(request.AntrianId, request.NoUrut);
            foreach (var map in queueMappings)
            {
                if (!map.PasienHadir)
                {
                    map.MarkPasienHadir(now);
                    _mappingRepo.Save(map);
                }
            }

            trans.Complete();
        }

        // 3. Menyiarkan sinyal prioritas ke worklist Telaah Resep via SignalR (TD-05)
        var entry = entryOpt.Value;
        var spOpt = _servicePointRepo.Get(entry.ServicePointId);
        var depoId = spOpt.HasValue ? spOpt.Value.DepoFarmasiId : string.Empty;
        await _publisher.PublishWorklistRefresh(depoId, cancellationToken);

        return new PharmacyQueueFlagPasienHadirResponse(
            request.DemandKind,
            request.DemandId,
            request.AntrianId,
            request.NoUrut,
            true,
            now,
            true);
    }
}

#endregion
