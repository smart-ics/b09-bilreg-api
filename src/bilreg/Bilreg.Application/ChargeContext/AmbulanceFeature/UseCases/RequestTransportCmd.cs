using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record RequestTransportCmd(
    TransportTypeEnum TipePerjalanan,
    ChargingMethodEnum MetodeTarif,
    string LayananId,
    string PetugasId,
    string? RegId = null,
    string? TujuanTransportId = null,
    decimal Konstanta = 0m,
    string LokasiAsal = "",
    string LokasiTujuan = "",
    string JaminanId = "",
    string Keterangan = "",
    string? TrsTransportId = null,
    DateTime? TglTrs = null,
    string? AmbulanceId = null,
    string? DriverId = null,
    string? NoPolisi = null,
    string? PerawatId = null,
    string? DokterId = null) : IRequest<RequestTransportResponse>;

public record RequestTransportResponse(
    string TrsTransportId,
    string Status);

public class RequestTransportHandler : IRequestHandler<RequestTransportCmd, RequestTransportResponse>
{
    private readonly ITrsTransportRepo _trsTransportRepo;
    private readonly ITujuanTransportRepo _tujuanTransportRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public RequestTransportHandler(
        ITrsTransportRepo trsTransportRepo,
        ITujuanTransportRepo tujuanTransportRepo,
        ITglJamProvider tglJamProvider)
    {
        _trsTransportRepo = trsTransportRepo;
        _tujuanTransportRepo = tujuanTransportRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<RequestTransportResponse> Handle(RequestTransportCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.PetugasId, nameof(request.PetugasId));

        var tujuanId = request.TujuanTransportId ?? string.Empty;
        var konstanta = request.Konstanta;

        if (request.MetodeTarif == ChargingMethodEnum.Wilayah)
        {
            if (string.IsNullOrWhiteSpace(tujuanId))
                throw new ArgumentException("Metode tarif Wilayah wajib menentukan TujuanTransportId.", nameof(request.TujuanTransportId));

            if (konstanta <= 0)
            {
                var tujuan = _tujuanTransportRepo.LoadEntity(new TujuanTransportKey(tujuanId)).Match(
                    onSome: t => t,
                    onNone: () => null);

                if (tujuan != null)
                    konstanta = tujuan.Konstanta;
            }
        }
        else if (request.MetodeTarif == ChargingMethodEnum.Jarak)
        {
            if (!string.IsNullOrWhiteSpace(tujuanId))
                throw new ArgumentException("Transaksi dengan metode tarif Jarak tidak boleh memiliki TujuanTransportId (Core Invariant 6).", nameof(request.TujuanTransportId));

            konstanta = 0m;
        }

        var tglTrs = request.TglTrs ?? _tglJamProvider.Now;

        var transport = TrsTransportModel.Create(
            request.TipePerjalanan,
            request.MetodeTarif,
            request.LayananId,
            request.PetugasId,
            trsTransportId: request.TrsTransportId,
            tglTrs: tglTrs,
            regId: request.RegId ?? string.Empty,
            tujuanTransportId: tujuanId,
            konstanta: konstanta,
            lokasiAsal: request.LokasiAsal,
            lokasiTujuan: request.LokasiTujuan,
            jaminanId: request.JaminanId,
            keterangan: request.Keterangan,
            ambulanceId: request.AmbulanceId ?? string.Empty,
            driverId: request.DriverId ?? string.Empty,
            noPolisi: request.NoPolisi ?? string.Empty,
            perawatId: request.PerawatId ?? string.Empty,
            dokterId: request.DokterId ?? string.Empty);

        _trsTransportRepo.SaveChanges(transport);

        return Task.FromResult(new RequestTransportResponse(transport.TrsTransportId, transport.Status.ToString()));
    }
}
