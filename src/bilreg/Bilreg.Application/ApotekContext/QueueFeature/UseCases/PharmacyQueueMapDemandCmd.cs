using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.DispensingFeature;
using Bilreg.Application.ApotekContext.DispensingFeature.UseCases;
using Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
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
            req.DemandIds,
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
    private readonly ITelaahResepRepo? _telaahRepo;
    private readonly IRegRepo? _regRepo;
    private readonly IMediator? _mediator;
    private readonly ISalesOrderRepo? _salesOrderRepo;
    private readonly ITrackerPharmacyPort? _trackerPort;

    public PharmacyQueueMapDemandHandler(
        IPharmacyQueueMappingRepo mappingRepo,
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyServicePointRepo servicePointRepo,
        IResepRepo resepRepo,
        IPharmacyQueuePublisher publisher,
        IAptAuthorizationPolicy auth,
        ITelaahResepRepo? telaahRepo = null,
        IRegRepo? regRepo = null,
        IMediator? mediator = null,
        ISalesOrderRepo? salesOrderRepo = null,
        ITrackerPharmacyPort? trackerPort = null)
    {
        _mappingRepo = mappingRepo;
        _queueEntryRepo = queueEntryRepo;
        _servicePointRepo = servicePointRepo;
        _resepRepo = resepRepo;
        _publisher = publisher;
        _auth = auth;
        _telaahRepo = telaahRepo;
        _regRepo = regRepo;
        _mediator = mediator;
        _salesOrderRepo = salesOrderRepo;
        _trackerPort = trackerPort;
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

        var entry = entryOpt.Value;

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
        var resepList = new List<ResepModel>();
        foreach (var id in demandIds)
        {
            if (request.DemandKind is QueueDemandKindEnum.Resep or QueueDemandKindEnum.ResepKerja)
            {
                var resep = _resepRepo.LoadEntity(ResepModel.Key(id))
                    .GetValueOrThrow($"Resep '{id}' tidak ditemukan.");
                resepList.Add(resep);
            }
        }

        // 4. Backfill RegId/PasienId jika entry.RegId masih kosong dan demand berupa ResepKerja
        if (request.DemandKind is QueueDemandKindEnum.Resep or QueueDemandKindEnum.ResepKerja)
        {
            if (string.IsNullOrWhiteSpace(entry.RegId) && resepList.Count > 0)
            {
                var firstResep = resepList[0];
                var backfillRegId = firstResep.Register?.RegId ?? "";
                var backfillPasienId = firstResep.Register?.PasienId ?? "";
                if (string.IsNullOrWhiteSpace(backfillPasienId) && !string.IsNullOrWhiteSpace(backfillRegId) && _regRepo != null)
                {
                    var regOpt = _regRepo.LoadEntity(RegModel.Key(backfillRegId));
                    if (regOpt.HasValue)
                        backfillPasienId = regOpt.Value.Pasien?.PasienId ?? "";
                }
                if (!string.IsNullOrWhiteSpace(backfillRegId) || !string.IsNullOrWhiteSpace(backfillPasienId))
                {
                    entry.AssignRegistration(backfillRegId, backfillPasienId);
                }
            }
        }

        // 5. Transisikan fase TAKEN/DILEWATI -> PENERIMAAN dan sinkronkan status makro via _trackerPort.ServeOnce
        var now = DateTime.Now;
        var primaryDemandId = demandIds.First();
        var trackerId = !string.IsNullOrWhiteSpace(request.PasienTrackerId) ? request.PasienTrackerId : "-";

        if (entry.ApotekPhase is ApotekPhaseEnum.TAKEN or ApotekPhaseEnum.DILEWATI)
        {
            entry.StartServicePenerimaan(now);
            _trackerPort?.ServeOnce(entry.AntrianId, entry.NoUrut, trackerId, primaryDemandId, now);
        }

        // 6. Petakan tiket antrean ke seluruh permintaan di dalam transaksi (otomatis panggil MarkPasienHadir)
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

            model.MarkPasienHadir(now);
            _mappingRepo.Save(model);
        }

        // 7. Evaluasi status TelaahResepModel bagi seluruh demand ResepKerja pada tiket
        bool allTelaahApproved = false;
        if (_telaahRepo != null && request.DemandKind is QueueDemandKindEnum.Resep or QueueDemandKindEnum.ResepKerja)
        {
            var allTicketMappings = _mappingRepo.ListByQueue(request.AntrianId, request.NoUrut);
            var ticketResepIds = allTicketMappings
                .Where(m => m.DemandKind is QueueDemandKindEnum.Resep or QueueDemandKindEnum.ResepKerja)
                .Select(m => m.DemandId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ticketResepIds.Count > 0)
            {
                allTelaahApproved = true;
                foreach (var rId in ticketResepIds)
                {
                    var telaahOpt = _telaahRepo.LoadByResep(rId);
                    if (!telaahOpt.HasValue || !telaahOpt.Value.CanEstablishSalesOrder)
                    {
                        allTelaahApproved = false;
                        break;
                    }
                }
            }
        }

        if (allTelaahApproved)
        {
            var regId = entry.RegId;
            if (string.IsNullOrWhiteSpace(regId) && resepList.Count > 0)
                regId = resepList[0].Register?.RegId ?? "";

            var regOpt = !string.IsNullOrWhiteSpace(regId) && _regRepo != null
                ? _regRepo.LoadEntity(RegModel.Key(regId))
                : MayBe<RegModel>.None;
            var reg = regOpt.HasValue ? regOpt.Value : null;

            var tipeJaminanId = reg?.TipeJaminan?.TipeJaminanId ?? resepList.FirstOrDefault()?.TipeBrg?.TipeBrgId ?? string.Empty;
            var tipeJaminanName = reg?.TipeJaminan?.TipeJaminanName ?? resepList.FirstOrDefault()?.TipeBrg?.TipeBrgName ?? string.Empty;

            bool isBpjs = string.Equals(tipeJaminanId, "JKN00", StringComparison.OrdinalIgnoreCase) ||
                          tipeJaminanId.Contains("BPJS", StringComparison.OrdinalIgnoreCase) ||
                          tipeJaminanName.Contains("BPJS", StringComparison.OrdinalIgnoreCase) ||
                          tipeJaminanName.Contains("Asuransi", StringComparison.OrdinalIgnoreCase);

            if (isBpjs)
            {
                if (_mediator != null)
                {
                    var actorUserId = string.IsNullOrWhiteSpace(request.UserId) ? "SYSTEM_AUTO" : request.UserId;
                    var allTicketMappings = _mappingRepo.ListByQueue(request.AntrianId, request.NoUrut);
                    var ticketResepIds = allTicketMappings
                        .Where(m => m.DemandKind is QueueDemandKindEnum.Resep or QueueDemandKindEnum.ResepKerja)
                        .Select(m => m.DemandId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var rId in ticketResepIds)
                    {
                        var duIds = await _mediator.Send(new PenjualanCreateFromTelaahCmd(
                            UserId: actorUserId,
                            ResepId: rId,
                            AntrianId: entry.AntrianId,
                            NoUrut: entry.NoUrut
                        ), cancellationToken);

                        if (_salesOrderRepo != null)
                        {
                            var existingSo = _salesOrderRepo.ListBySource(SalesOrderSourceKindEnum.Resep, rId)?.FirstOrDefault();
                            string salesOrderId;
                            SalesOrderModel? soModel = null;

                            if (existingSo != null)
                            {
                                salesOrderId = existingSo.SalesOrderId;
                                soModel = existingSo;
                            }
                            else
                            {
                                var soResp = await _mediator.Send(new SalesOrderEstablishCmd(
                                    UserId: actorUserId,
                                    SourceKind: SalesOrderSourceKindEnum.Resep,
                                    SourceId: rId,
                                    PayerPath: PayerPathEnum.Bpjs,
                                    PartialReason: PartialReasonEnum.None,
                                    QtyOverrides: null
                                ), cancellationToken);

                                salesOrderId = soResp?.SalesOrderId ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(salesOrderId))
                                {
                                    var soOpt = _salesOrderRepo.LoadEntity(SalesOrderModel.Key(salesOrderId));
                                    if (soOpt.HasValue)
                                        soModel = soOpt.Value;
                                }
                            }

                            if (soModel != null)
                            {
                                var dispensingItems = soModel.Items
                                    .Where(x => x.UnresolvedAcceptedQty > 0)
                                    .Select(x => new DispensingEstablishItem(x.ItemNo, x.UnresolvedAcceptedQty))
                                    .ToList();

                                if (dispensingItems.Count > 0)
                                {
                                    await _mediator.Send(new DispensingEstablishCmd(
                                        UserId: actorUserId,
                                        SalesOrderId: salesOrderId,
                                        Items: dispensingItems,
                                        PenjualanId: duIds?.FirstOrDefault()
                                    ), cancellationToken);
                                }
                            }
                        }
                    }
                }

                if (entry.ApotekPhase is ApotekPhaseEnum.TAKEN or ApotekPhaseEnum.PENERIMAAN or ApotekPhaseEnum.DILEWATI)
                {
                    entry.TransitionToPenyiapan();
                }
            }
        }

        _queueEntryRepo.SaveChanges(entry);
        trans.Complete();

        // 8. Siarkan hint penyegaran worklist ke depo SignalR
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
