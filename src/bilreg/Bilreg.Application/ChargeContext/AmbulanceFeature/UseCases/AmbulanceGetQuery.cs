using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record AmbulanceGetQuery(string AmbulanceId) : IRequest<AmbulanceGetResponse>, IAmbulanceKey;

public record AmbulanceGetResponse(
    string AmbulanceId,
    string AmbulanceName,
    bool IsAktif,
    decimal Abonement,
    string NoPolisi,
    string TipeArmada,
    string StatusArmada,
    IEnumerable<AmbulanceKomponenResponse> ListKomponen,
    IEnumerable<AmbulanceTujuanResponse> ListTujuan);

public record AmbulanceKomponenResponse(
    string DetilTarifId,
    decimal Tarif,
    bool IsTetap);

public record AmbulanceTujuanResponse(
    string TujuanTransportId,
    decimal Konstanta);

public class AmbulanceGetHandler : IRequestHandler<AmbulanceGetQuery, AmbulanceGetResponse>
{
    private readonly IAmbulanceRepo _ambulanceRepo;

    public AmbulanceGetHandler(IAmbulanceRepo ambulanceRepo)
    {
        _ambulanceRepo = ambulanceRepo;
    }

    public Task<AmbulanceGetResponse> Handle(AmbulanceGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));

        var ambulance = _ambulanceRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Ambulans dengan id '{request.AmbulanceId}' tidak ditemukan."));

        var listKomponen = ambulance.ListKomponen
            .Select(k => new AmbulanceKomponenResponse(k.KomponenId, k.NilaiTarif, k.IsTetap))
            .ToList();

        var listTujuan = ambulance.ListTujuan
            .Select(t => new AmbulanceTujuanResponse(t.TujuanTransportId, t.Konstanta))
            .ToList();

        var response = new AmbulanceGetResponse(
            ambulance.AmbulanceId,
            ambulance.AmbulanceName,
            ambulance.IsAktif,
            ambulance.Abonement,
            ambulance.NoPolisi,
            ambulance.TipeArmada,
            ambulance.StatusArmada,
            listKomponen,
            listTujuan);

        return Task.FromResult(response);
    }
}
