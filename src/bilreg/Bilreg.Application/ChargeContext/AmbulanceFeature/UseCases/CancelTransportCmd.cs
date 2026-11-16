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

public record CancelTransportCmd(
    string TrsTransportId,
    string AlasanBatal,
    decimal? BiayaBatal = null,
    string PetugasVoidId = "",
    DateTime? CancelTime = null) : IRequest<CancelTransportResponse>, ITrsTransportKey;

public record CancelTransportResponse(
    string TrsTransportId,
    string Status,
    decimal BiayaBatal,
    bool IsPostedBilling);

public class CancelTransportHandler : IRequestHandler<CancelTransportCmd, CancelTransportResponse>
{
    private readonly ITrsTransportRepo _trsTransportRepo;
    private readonly IAmbulanceRepo _ambulanceRepo;
    private readonly IAddBillAppService _addBillAppService;
    private readonly IRegRepo _regRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public CancelTransportHandler(
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

    public Task<CancelTransportResponse> Handle(CancelTransportCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TrsTransportId, nameof(request.TrsTransportId));
        Guard.Against.NullOrWhiteSpace(request.AlasanBatal, nameof(request.AlasanBatal));

        var transport = _trsTransportRepo.LoadEntity(request).Match(
            onSome: t => t,
            onNone: () => throw new KeyNotFoundException($"Transaksi transport dengan id '{request.TrsTransportId}' tidak ditemukan."));

        var cancelTime = request.CancelTime ?? _tglJamProvider.Now;
        var biayaBatal = 0m;

        if (transport.Status == TransportStatusEnum.Dispatched)
        {
            if (request.BiayaBatal.HasValue)
            {
                biayaBatal = request.BiayaBatal.Value;
            }
            else if (!string.IsNullOrWhiteSpace(transport.AmbulanceId))
            {
                var ambulance = _ambulanceRepo.LoadEntity(new AmbulanceKey(transport.AmbulanceId)).Match(
                    onSome: a => a,
                    onNone: () => null);

                biayaBatal = ambulance?.Abonement ?? 0m;
            }
        }

        transport.Cancel(request.AlasanBatal, biayaBatal, request.PetugasVoidId, cancelTime);

        using var scope = TransHelper.NewScope();

        if (!transport.IsNonReg && !string.IsNullOrWhiteSpace(transport.RegId) && biayaBatal > 0)
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

            var listReffKomp = Enumerable.Empty<KomponenType>();

            _addBillAppService.FromAmbulanceUsage(
                transport,
                reg,
                jaminan,
                listReffKomp,
                createdAt: cancelTime);

            transport.SetPostedBilling();
        }

        _trsTransportRepo.SaveChanges(transport);
        scope.Complete();

        return Task.FromResult(new CancelTransportResponse(
            transport.TrsTransportId,
            transport.Status.ToString(),
            transport.BiayaBatal,
            transport.IsPostedBilling));
    }
}
