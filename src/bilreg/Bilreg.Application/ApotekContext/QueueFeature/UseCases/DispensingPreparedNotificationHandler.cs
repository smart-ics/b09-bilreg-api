using Bilreg.Application.ApotekContext.DispensingFeature;
using Bilreg.Application.ApotekContext.DispensingFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

/// <summary>
/// Handler notifikasi penyelesaian penyiapan dispensing obat (TD-08).
/// Mengevaluasi konsolidasi multi-dispensing pada nomor antrean:
/// Jika seluruh dokumen dispensing yang dipetakan ke nomor tiket antrean telah mencapai status Prepared,
/// fase antrean ditransisikan secara otomatis dari PENYIAPAN ke SIAP_SERAH dan notifikasi disiarkan ke Loket Serah Obat.
/// </summary>
public class DispensingPreparedNotificationHandler : INotificationHandler<DispensingPreparedNotification>
{
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IDispensingRepo _dispensingRepo;
    private readonly IPharmacyQueueMappingRepo _mappingRepo;
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyServicePointRepo _servicePointRepo;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly ITglJamProvider _clock;

    public DispensingPreparedNotificationHandler(
        ISalesOrderRepo salesOrderRepo,
        IDispensingRepo dispensingRepo,
        IPharmacyQueueMappingRepo mappingRepo,
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyServicePointRepo servicePointRepo,
        IPharmacyQueuePublisher publisher,
        ITglJamProvider clock)
    {
        _salesOrderRepo = salesOrderRepo;
        _dispensingRepo = dispensingRepo;
        _mappingRepo = mappingRepo;
        _queueEntryRepo = queueEntryRepo;
        _servicePointRepo = servicePointRepo;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task Handle(DispensingPreparedNotification notification, CancellationToken cancellationToken)
    {
        // 1. Identifikasi Sales Order terkait
        var soOpt = _salesOrderRepo.LoadEntity(SalesOrderModel.Key(notification.SalesOrderId));
        if (!soOpt.HasValue)
            return;

        var so = soOpt.Value;

        // 2. Identifikasi dokumen permintaan (Demand) dan pemetaan tiket antrean
        var demandKind = so.SourceKind switch
        {
            SalesOrderSourceKindEnum.JualBebas => QueueDemandKindEnum.JualBebas,
            SalesOrderSourceKindEnum.ResepKerja => QueueDemandKindEnum.ResepKerja,
            _ => QueueDemandKindEnum.Resep
        };

        var mappingOpt = _mappingRepo.GetByDemand(demandKind, so.SourceId);
        if (!mappingOpt.HasValue && demandKind == QueueDemandKindEnum.Resep)
        {
            // Fallback: periksa pemetaan di bawah jenis ResepKerja
            mappingOpt = _mappingRepo.GetByDemand(QueueDemandKindEnum.ResepKerja, so.SourceId);
        }

        if (!mappingOpt.HasValue)
            return;

        var mapping = mappingOpt.Value;
        var antrianId = mapping.AntrianId;
        var noUrut = mapping.NoUrut;

        if (string.IsNullOrWhiteSpace(antrianId) || noUrut <= 0)
            return;

        // 3. Periksa entri antrean farmasi
        var entryOpt = _queueEntryRepo.Get(antrianId, noUrut);
        if (!entryOpt.HasValue)
            return;

        var entry = entryOpt.Value;

        // Konsolidasi ke SIAP_SERAH hanya berlaku jika antrean berada pada fase PENYIAPAN
        if (entry.ApotekPhase != ApotekPhaseEnum.PENYIAPAN)
            return;

        // 4. Evaluasi multi-dispensing: kumpulkan seluruh dokumen dispensing untuk tiket ini
        var queueMappings = _mappingRepo.ListByQueue(antrianId, noUrut) ?? [];
        var allDispensings = new List<DispensingModel>();

        foreach (var map in queueMappings)
        {
            var sk = map.DemandKind switch
            {
                QueueDemandKindEnum.JualBebas => SalesOrderSourceKindEnum.JualBebas,
                QueueDemandKindEnum.ResepKerja => SalesOrderSourceKindEnum.ResepKerja,
                _ => SalesOrderSourceKindEnum.Resep
            };

            var orders = _salesOrderRepo.ListBySource(sk, map.DemandId) ?? [];
            var activeOrders = orders.Where(x => x.IsActiveKey).ToList();
            foreach (var activeSo in activeOrders)
            {
                var dispensings = _dispensingRepo.ListBySalesOrder(activeSo.SalesOrderId) ?? [];
                allDispensings.AddRange(dispensings);
            }
        }

        if (allDispensings.Count == 0)
            return;

        // Pastikan seluruh dispensing aktif berstatus Prepared atau telah terselesaikan secara akuntabel,
        // dan setidaknya salah satu berstatus Prepared
        var allPrepared = allDispensings.All(d => d.IsPrepared || d.IsAccountablyResolved)
                          && allDispensings.Any(d => d.IsPrepared);

        if (!allPrepared)
            return;

        // 5. Transisi ke SIAP_SERAH
        var now = _clock.Now;
        entry.ConsolidateToSiapSerah(now);

        using (var trans = TransHelper.NewScope())
        {
            _queueEntryRepo.Save(entry);
            trans.Complete();
        }

        // 6. Siarkan notifikasi SignalR ke worklist Loket Serah Obat (Loket 2)
        var spOpt = _servicePointRepo.Get(entry.ServicePointId);
        var depoId = spOpt.HasValue ? spOpt.Value.DepoFarmasiId : "";
        await _publisher.PublishWorklistRefresh(depoId, cancellationToken);
    }
}
