using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record ChargeTransportCmd(
    string TrsTransportId,
    string PetugasId = "",
    DateTime? ChargeTime = null,
    string? DriverId = null,
    string? PerawatId = null,
    string? DokterId = null,
    IEnumerable<TransportItemAdjustmentDto>? Items = null) : IRequest<ChargeTransportResponse>, ITrsTransportKey;

public record ChargeTransportResponse(
    string TrsTransportId,
    string Status,
    decimal Total,
    bool IsPostedBilling);

public class ChargeTransportHandler : IRequestHandler<ChargeTransportCmd, ChargeTransportResponse>
{
    private readonly ITrsTransportRepo _trsTransportRepo;
    private readonly IAmbulanceRepo _ambulanceRepo;
    private readonly ITujuanTransportRepo _tujuanTransportRepo;
    private readonly IAddBillAppService _addBillAppService;
    private readonly IRegRepo _regRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public ChargeTransportHandler(
        ITrsTransportRepo trsTransportRepo,
        IAmbulanceRepo ambulanceRepo,
        ITujuanTransportRepo tujuanTransportRepo,
        IAddBillAppService addBillAppService,
        IRegRepo regRepo,
        IJaminanRepo jaminanRepo,
        IKomponenRepo komponenRepo,
        ITglJamProvider tglJamProvider)
    {
        _trsTransportRepo = trsTransportRepo;
        _ambulanceRepo = ambulanceRepo;
        _tujuanTransportRepo = tujuanTransportRepo;
        _addBillAppService = addBillAppService;
        _regRepo = regRepo;
        _jaminanRepo = jaminanRepo;
        _komponenRepo = komponenRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<ChargeTransportResponse> Handle(ChargeTransportCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TrsTransportId, nameof(request.TrsTransportId));

        var transport = _trsTransportRepo.LoadEntity(request).Match(
            onSome: t => t,
            onNone: () => throw new KeyNotFoundException($"Transaksi transport dengan id '{request.TrsTransportId}' tidak ditemukan."));

        if (transport.IsNonReg || string.IsNullOrWhiteSpace(transport.RegId))
            throw new ArgumentException("Transaksi ambulans non-registrasi tidak dapat diposting ke Billing.");

        if (transport.Status == TransportStatusEnum.Cancelled)
            throw new InvalidOperationException("Transaksi transport yang telah dibatalkan tidak dapat dibebankan ke Billing.");

        if (transport.IsPostedBilling)
            throw new InvalidOperationException("Transaksi transport sudah dibebankan ke Billing sebelumnya.");

        var reg = _regRepo.LoadEntity(new RegKey(transport.RegId)).Match(
            onSome: r => r,
            onNone: () => throw new KeyNotFoundException($"Registrasi '{transport.RegId}' untuk penagihan tidak ditemukan."));

        var jaminanId = !string.IsNullOrWhiteSpace(transport.JaminanId)
            ? transport.JaminanId
            : (reg.TipeJaminan?.TipeJaminanId ?? string.Empty);

        var jaminan = string.IsNullOrWhiteSpace(jaminanId)
            ? JaminanType.Default
            : _jaminanRepo.LoadEntity(JaminanType.Key(jaminanId)).Match(
                onSome: j => j,
                onNone: () => JaminanType.Default);

        if (transport.ListItems.Count == 0
            && !string.IsNullOrWhiteSpace(transport.AmbulanceId)
            && !string.IsNullOrWhiteSpace(transport.TujuanTransportId))
        {
            var ambulance = _ambulanceRepo.LoadEntity(new AmbulanceKey(transport.AmbulanceId)).Match(
                onSome: a => a,
                onNone: () => null);
            var tujuan = _tujuanTransportRepo.LoadEntity(new TujuanTransportKey(transport.TujuanTransportId)).Match(
                onSome: t => t,
                onNone: () => null);

            if (ambulance != null && tujuan != null)
            {
                var calc = TransportTariffCalculator.Calculate(ambulance, tujuan);
                foreach (var item in calc.Items)
                {
                    transport.AddItem(new TrsTransportItemModel(
                        transport.TrsTransportId,
                        item.DetilTarifId,
                        item.TarifFinal,
                        1m));
                }
            }
        }

        var chargeTime = request.ChargeTime ?? _tglJamProvider.Now;

        var kompKeys = transport.ListItems
            .Select(x => KomponenType.Key(x.DetilTarifId))
            .Distinct();

        var listReffKomp = _komponenRepo.ListData(kompKeys)?.ToList() ?? [];

        using var scope = TransHelper.NewScope();

        if (request.DriverId is not null || request.PerawatId is not null || request.DokterId is not null)
        {
            transport.UpdateCrew(request.DriverId, request.PerawatId, request.DokterId);
        }

        if (request.Items != null)
        {
            var adjustments = request.Items.Select(x => (x.DetilTarifId, x.Tarif, x.Diskon));
            transport.AdjustItems(adjustments);
        }

        _addBillAppService.FromAmbulanceUsage(
            transport,
            reg,
            jaminan,
            listReffKomp,
            createdAt: chargeTime);

        transport.SetPostedBilling();

        _trsTransportRepo.SaveChanges(transport);
        scope.Complete();

        return Task.FromResult(new ChargeTransportResponse(
            transport.TrsTransportId,
            transport.Status.ToString(),
            transport.Total,
            transport.IsPostedBilling));
    }
}
