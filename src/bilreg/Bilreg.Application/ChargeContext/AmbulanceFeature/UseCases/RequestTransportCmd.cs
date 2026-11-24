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
    string? DokterId = null,
    decimal? KmRate = null,
    decimal? TotalJarakKm = null) : IRequest<RequestTransportResponse>;

public record RequestTransportResponse(
    string TrsTransportId,
    string Status);

public class RequestTransportHandler : IRequestHandler<RequestTransportCmd, RequestTransportResponse>
{
    private readonly ITrsTransportRepo _trsTransportRepo;
    private readonly ITujuanTransportRepo _tujuanTransportRepo;
    private readonly IAmbulanceRepo _ambulanceRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public RequestTransportHandler(
        ITrsTransportRepo trsTransportRepo,
        ITujuanTransportRepo tujuanTransportRepo,
        IAmbulanceRepo ambulanceRepo,
        ITglJamProvider tglJamProvider)
    {
        _trsTransportRepo = trsTransportRepo;
        _tujuanTransportRepo = tujuanTransportRepo;
        _ambulanceRepo = ambulanceRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<RequestTransportResponse> Handle(RequestTransportCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.PetugasId, nameof(request.PetugasId));

        if (request.MetodeTarif == ChargingMethodEnum.Jarak)
            throw new ArgumentException("Metode tarif Jarak tidak didukung; perhitungan tarif transport menggunakan metode Tujuan/Wilayah.", nameof(request.MetodeTarif));

        var tujuanId = request.TujuanTransportId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tujuanId))
            throw new ArgumentException("Metode tarif Wilayah wajib menentukan TujuanTransportId.", nameof(request.TujuanTransportId));

        var tujuan = _tujuanTransportRepo.LoadEntity(new TujuanTransportKey(tujuanId)).Match(
            onSome: t => t,
            onNone: () => null);

        var ambulance = !string.IsNullOrWhiteSpace(request.AmbulanceId)
            ? _ambulanceRepo.LoadEntity(new AmbulanceKey(request.AmbulanceId)).Match(
                onSome: a => a,
                onNone: () => null)
            : null;

        TransportTariffCalculationResult? calcResult = null;
        var konstanta = tujuan?.Konstanta ?? 0m;

        if (ambulance != null && tujuan != null)
        {
            calcResult = TransportTariffCalculator.Calculate(ambulance, tujuan);
            konstanta = calcResult.KonstantaEfektif;
        }

        var tglTrs = request.TglTrs ?? _tglJamProvider.Now;

        var transport = TrsTransportModel.Create(
            request.TipePerjalanan,
            ChargingMethodEnum.Wilayah,
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

        if (calcResult != null)
        {
            foreach (var item in calcResult.Items)
            {
                transport.AddItem(new TrsTransportItemModel(transport.TrsTransportId, item.DetilTarifId, item.TarifFinal, 1m));
            }
        }

        _trsTransportRepo.SaveChanges(transport);

        return Task.FromResult(new RequestTransportResponse(transport.TrsTransportId, transport.Status.ToString()));
    }
}
