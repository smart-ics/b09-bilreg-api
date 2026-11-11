using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.DispensingFeature;
using Bilreg.Application.ApotekContext.DispensingFeature.UseCases;
using Bilreg.Application.ApotekContext.PenjualanFeature;
using Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

/// <summary>
/// In-process event handler saat TelaahCompleteCmd berhasil disahkan (TD-05):
/// Jika resep tertaut ke tiket antrean dengan status PasienHadir == true:
/// - Pasien BPJS/Asuransi: secara otomatis mengeksekusi PenjualanCreateFromTelaahCmd (menerbitkan tagihan komersial DU...)
///   dan menginisialisasi DispensingEstablishCmd (order penyiapan fisik obat), lalu mentransisikan fase tiket ke PENYIAPAN.
/// - Pasien Mandiri (Umum): tiket antrean tetap pada fase penerimaan dan secara otomatis muncul pada tab 'Siap Penjualan'
///   pada worklist Loket 1 untuk konfirmasi rincian biaya.
/// </summary>
public class TelaahCompletedAutoBpjsPenjualanHandler : INotificationHandler<TelaahCompletedNotification>
{
    private readonly IMediator _mediator;
    private readonly IPharmacyQueueMappingRepo _mappingRepo;
    private readonly IApotekQueueEntryRepo _queueEntryRepo;
    private readonly IPharmacyServicePointRepo _servicePointRepo;
    private readonly IResepRepo _resepRepo;
    private readonly IRegRepo _regRepo;
    private readonly ISalesOrderRepo _salesOrderRepo;
    private readonly IPharmacyQueuePublisher _publisher;
    private readonly ILogger<TelaahCompletedAutoBpjsPenjualanHandler> _logger;

    public TelaahCompletedAutoBpjsPenjualanHandler(
        IMediator mediator,
        IPharmacyQueueMappingRepo mappingRepo,
        IApotekQueueEntryRepo queueEntryRepo,
        IPharmacyServicePointRepo servicePointRepo,
        IResepRepo resepRepo,
        IRegRepo regRepo,
        ISalesOrderRepo salesOrderRepo,
        IPharmacyQueuePublisher publisher,
        ILogger<TelaahCompletedAutoBpjsPenjualanHandler>? logger = null)
    {
        _mediator = mediator;
        _mappingRepo = mappingRepo;
        _queueEntryRepo = queueEntryRepo;
        _servicePointRepo = servicePointRepo;
        _resepRepo = resepRepo;
        _regRepo = regRepo;
        _salesOrderRepo = salesOrderRepo;
        _publisher = publisher;
        _logger = logger ?? NullLogger<TelaahCompletedAutoBpjsPenjualanHandler>.Instance;
    }

    public async Task Handle(TelaahCompletedNotification notification, CancellationToken cancellationToken)
    {
        // 1. Hanya proses jika telaah disetujui (Approved atau PartiallyApproved)
        if (notification.TelaahStatus is not (TelaahStatusEnum.Approved or TelaahStatusEnum.PartiallyApproved))
            return;

        try
        {
            // 2. Periksa apakah resep memiliki tiket antrean yang terpetakan
            var mappingOpt = _mappingRepo.GetByDemand(QueueDemandKindEnum.Resep, notification.ResepId);
            if (!mappingOpt.HasValue)
                mappingOpt = _mappingRepo.GetByDemand(QueueDemandKindEnum.ResepKerja, notification.ResepId);

            if (!mappingOpt.HasValue)
                return;

            var mapping = mappingOpt.Value;

            // 3. Evaluasi kriteria PasienHadir
            if (!mapping.PasienHadir)
                return;

            // 4. Identifikasi entri antrean farmasi & depo
            var entryOpt = _queueEntryRepo.Get(mapping.AntrianId, mapping.NoUrut);
            if (!entryOpt.HasValue)
                return;

            var entry = entryOpt.Value;
            var spOpt = _servicePointRepo.Get(entry.ServicePointId);
            var depoId = spOpt.HasValue ? spOpt.Value.DepoFarmasiId : string.Empty;

            // 5. Periksa kelompok penjamin (BPJS/Asuransi vs Umum)
            var resepOpt = _resepRepo.LoadEntity(ResepModel.Key(notification.ResepId));
            if (!resepOpt.HasValue)
                return;

            var resep = resepOpt.Value;
            var regId = resep.Register?.RegId ?? string.Empty;
            var regOpt = !string.IsNullOrWhiteSpace(regId) ? _regRepo.LoadEntity(RegModel.Key(regId)) : MayBe<RegModel>.None;
            var reg = regOpt.HasValue ? regOpt.Value : null;

            var tipeJaminanId = reg?.TipeJaminan?.TipeJaminanId ?? resep.TipeBrg?.TipeBrgId ?? string.Empty;
            var tipeJaminanName = reg?.TipeJaminan?.TipeJaminanName ?? resep.TipeBrg?.TipeBrgName ?? string.Empty;

            bool isBpjs = string.Equals(tipeJaminanId, "JKN00", StringComparison.OrdinalIgnoreCase) ||
                         tipeJaminanId.Contains("BPJS", StringComparison.OrdinalIgnoreCase) ||
                         tipeJaminanName.Contains("BPJS", StringComparison.OrdinalIgnoreCase) ||
                         tipeJaminanName.Contains("Asuransi", StringComparison.OrdinalIgnoreCase);

            if (!isBpjs)
            {
                // Pasien Mandiri (Umum):
                // Antrean tetap pada fase penerimaan dan secara otomatis masuk ke tab 'Siap Penjualan' di Loket 1.
                await _publisher.PublishWorklistRefresh(depoId, cancellationToken);
                return;
            }

            // Pasien BPJS / Asuransi:
            // Eksekusi otomatis PenjualanCreateFromTelaahCmd & inisialisasi DispensingEstablishCmd
            var actorUserId = string.IsNullOrWhiteSpace(notification.UserId) ? "SYSTEM_AUTO" : notification.UserId;

            // A. Terbitkan tagihan penjualan DU... dari telaah
            var duIds = await _mediator.Send(new PenjualanCreateFromTelaahCmd(
                UserId: actorUserId,
                ResepId: notification.ResepId
            ), cancellationToken);

            // B. Siapkan SalesOrder dan Dispensing
            var existingSo = _salesOrderRepo.ListBySource(SalesOrderSourceKindEnum.Resep, notification.ResepId).FirstOrDefault();
            string salesOrderId;
            SalesOrderModel soModel;

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
                    SourceId: notification.ResepId,
                    PayerPath: PayerPathEnum.Bpjs,
                    PartialReason: PartialReasonEnum.None,
                    QtyOverrides: null
                ), cancellationToken);

                salesOrderId = soResp.SalesOrderId;
                soModel = _salesOrderRepo.LoadEntity(SalesOrderModel.Key(salesOrderId))
                    .GetValueOrThrow($"Sales Order '{salesOrderId}' tidak ditemukan setelah pembentukan.");
            }

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

            // C. Transisi fase antrean farmasi ke PENYIAPAN
            var currentEntryOpt = _queueEntryRepo.Get(mapping.AntrianId, mapping.NoUrut);
            if (currentEntryOpt.HasValue)
            {
                var currentEntry = currentEntryOpt.Value;
                if (currentEntry.ApotekPhase is ApotekPhaseEnum.TAKEN or ApotekPhaseEnum.PENERIMAAN or ApotekPhaseEnum.DILEWATI)
                {
                    currentEntry.TransitionToPenyiapan();
                    using var trans = TransHelper.NewScope();
                    _queueEntryRepo.SaveChanges(currentEntry);
                    trans.Complete();
                }
            }

            // D. Siarkan sinyal refresh worklist via SignalR
            await _publisher.PublishWorklistRefresh(depoId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Gagal menjalankan otomasi BPJS penjualan/dispensing untuk Resep '{ResepId}'",
                notification.ResepId);
            // Fail-safe boundary: log without failing the primary clinical TelaahComplete mutation
        }
    }
}
