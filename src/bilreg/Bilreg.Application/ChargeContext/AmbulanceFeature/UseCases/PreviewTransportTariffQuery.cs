using Ardalis.GuardClauses;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record PreviewTransportTariffQuery(
    string AmbulanceId,
    string TujuanTransportId) : IRequest<PreviewTransportTariffResponse>;

public record PreviewTransportTariffItemResponse(
    string DetilTarifId,
    string DetilTarifName,
    bool IsTetap,
    decimal Tarif);

public record PreviewTransportTariffResponse(
    string AmbulanceId,
    string TujuanTransportId,
    decimal Konstanta,
    decimal Abonement,
    decimal Total,
    IEnumerable<PreviewTransportTariffItemResponse> ListKomponen);

public class PreviewTransportTariffHandler : IRequestHandler<PreviewTransportTariffQuery, PreviewTransportTariffResponse>
{
    private readonly IAmbulanceRepo _ambulanceRepo;
    private readonly ITujuanTransportRepo _tujuanTransportRepo;
    private readonly IKomponenRepo _komponenRepo;

    public PreviewTransportTariffHandler(
        IAmbulanceRepo ambulanceRepo,
        ITujuanTransportRepo tujuanTransportRepo,
        IKomponenRepo komponenRepo)
    {
        _ambulanceRepo = ambulanceRepo;
        _tujuanTransportRepo = tujuanTransportRepo;
        _komponenRepo = komponenRepo;
    }

    public Task<PreviewTransportTariffResponse> Handle(PreviewTransportTariffQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));
        Guard.Against.NullOrWhiteSpace(request.TujuanTransportId, nameof(request.TujuanTransportId));

        var ambulance = _ambulanceRepo.LoadEntity(new AmbulanceKey(request.AmbulanceId)).Match(
            onSome: a => a,
            onNone: () => throw new KeyNotFoundException($"Ambulance dengan id '{request.AmbulanceId}' tidak ditemukan."));

        var tujuan = _tujuanTransportRepo.LoadEntity(new TujuanTransportKey(request.TujuanTransportId)).Match(
            onSome: t => t,
            onNone: () => throw new KeyNotFoundException($"Tujuan transport dengan id '{request.TujuanTransportId}' tidak ditemukan."));

        var calc = TransportTariffCalculator.Calculate(ambulance, tujuan);

        var kompKeys = calc.Items
            .Select(x => (IKomponenKey)KomponenType.Key(x.DetilTarifId))
            .DistinctBy(k => k.KomponenId)
            .ToList();

        var komponenNames = kompKeys.Count > 0
            ? (_komponenRepo.ListData(kompKeys) ?? []).ToDictionary(k => k.KomponenId, k => k.KomponenName)
            : new Dictionary<string, string>();

        var listKomponen = calc.Items.Select(item => new PreviewTransportTariffItemResponse(
            item.DetilTarifId,
            komponenNames.GetValueOrDefault(item.DetilTarifId, string.Empty),
            item.IsTetap,
            item.TarifFinal)).ToList();

        return Task.FromResult(new PreviewTransportTariffResponse(
            ambulance.AmbulanceId,
            tujuan.TujuanTransportId,
            calc.KonstantaEfektif,
            calc.Abonement,
            calc.Total,
            listKomponen));
    }
}
