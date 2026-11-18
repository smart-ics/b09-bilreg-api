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

public record CompleteTransportCmd(
    string TrsTransportId,
    decimal EndOdometer,
    DateTime? ReturnTime = null,
    IEnumerable<TrsTransportItemInputDto>? Items = null) : IRequest<CompleteTransportResponse>, ITrsTransportKey;

public record TrsTransportItemInputDto(
    string DetilTarifId,
    decimal Tarif,
    decimal Qty = 1m,
    string PetugasMedisId = "",
    decimal Diskon = 0m,
    string Keterangan = "");

public record CompleteTransportResponse(
    string TrsTransportId,
    string Status,
    decimal Total,
    bool IsPostedBilling);

public class CompleteTransportHandler : IRequestHandler<CompleteTransportCmd, CompleteTransportResponse>
{
    private readonly ITrsTransportRepo _trsTransportRepo;
    private readonly IAmbulanceRepo _ambulanceRepo;
    private readonly IAddBillAppService _addBillAppService;
    private readonly IRegRepo _regRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public CompleteTransportHandler(
        ITrsTransportRepo trsTransportRepo,
        IAmbulanceRepo ambulanceRepo,
        IAddBillAppService addBillAppService,
        IRegRepo regRepo,
        IJaminanRepo jaminanRepo,
        IKomponenRepo komponenRepo,
        ITglJamProvider tglJamProvider)
    {
        _trsTransportRepo = trsTransportRepo;
        _ambulanceRepo = ambulanceRepo;
        _addBillAppService = addBillAppService;
        _regRepo = regRepo;
        _jaminanRepo = jaminanRepo;
        _komponenRepo = komponenRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<CompleteTransportResponse> Handle(CompleteTransportCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TrsTransportId, nameof(request.TrsTransportId));

        var transport = _trsTransportRepo.LoadEntity(request).Match(
            onSome: t => t,
            onNone: () => throw new KeyNotFoundException($"Transaksi transport dengan id '{request.TrsTransportId}' tidak ditemukan."));

        var returnTime = request.ReturnTime ?? _tglJamProvider.Now;

        // Populate items if passed before marking completed (since AddItem checks terminal state)
        if (request.Items != null)
        {
            foreach (var item in request.Items)
            {
                transport.AddItem(new TrsTransportItemModel(
                    transport.TrsTransportId,
                    item.DetilTarifId,
                    item.Tarif,
                    item.Qty,
                    item.PetugasMedisId,
                    item.Diskon,
                    item.Keterangan));
            }
        }
        else if (transport.ListItems.Count == 0 && !string.IsNullOrWhiteSpace(transport.AmbulanceId))
        {
            // Default components from ambulance master if no items recorded
            var ambulance = _ambulanceRepo.LoadEntity(new AmbulanceKey(transport.AmbulanceId)).Match(
                onSome: a => a,
                onNone: () => null);

            if (ambulance != null && ambulance.ListKomponen.Count > 0)
            {
                foreach (var komp in ambulance.ListKomponen)
                {
                    transport.AddItem(new TrsTransportItemModel(
                        transport.TrsTransportId,
                        komp.KomponenId,
                        komp.NilaiTarif,
                        1m));
                }
            }
        }

        // Domain validation: EndOdometer >= StartOdometer & Status == Dispatched
        transport.Complete(request.EndOdometer, returnTime);

        using var scope = TransHelper.NewScope();

        // Financial settlement: dual-path
        if (!transport.IsNonReg && !string.IsNullOrWhiteSpace(transport.RegId))
        {
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

            var kompKeys = transport.ListItems
                .Select(x => KomponenType.Key(x.DetilTarifId))
                .Distinct();

            var listReffKomp = _komponenRepo.ListData(kompKeys)?.ToList() ?? [];

            _addBillAppService.FromAmbulanceUsage(
                transport,
                reg,
                jaminan,
                listReffKomp,
                createdAt: returnTime);

            transport.SetPostedBilling();
        }

        _trsTransportRepo.SaveChanges(transport);
        scope.Complete();

        return Task.FromResult(new CompleteTransportResponse(
            transport.TrsTransportId,
            transport.Status.ToString(),
            transport.Total,
            transport.IsPostedBilling));
    }
}
