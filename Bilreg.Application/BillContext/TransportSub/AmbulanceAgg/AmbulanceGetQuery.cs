using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using MediatR;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceGetQuery(string AmbulanceId) : IRequest<AmbulanceGetResponse>, IAmbulanceKey;

public record AmbulanceKomponenGetResponse(
    string AmbulanceId,
    string KomponenId,
    decimal NilaiTarif,
    bool IsTetap
);

public record AmbulanceGetResponse(
    string AmbulanceId,
    string AmbulanceName,
    bool IsAktif,
    decimal Abonement,
    IEnumerable<AmbulanceKomponenGetResponse> ListKomponen
);

public class AmbulanceGetHandler : IRequestHandler<AmbulanceGetQuery, AmbulanceGetResponse>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;

    public AmbulanceGetHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory)
    {
        _factory = factory;
    }

    public Task<AmbulanceGetResponse> Handle(AmbulanceGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var ambulance = _factory.Load(request);

        // RESPONSE
        var listKomponen = ambulance.ListKomponen.Select(x =>
            new AmbulanceKomponenGetResponse(x.AmbulanceId, x.KomponenId, x.NilaiTarif, x.IsTetap));
        var response = new AmbulanceGetResponse(ambulance.AmbulanceId, ambulance.AmbulanceName, ambulance.IsAktif,
            ambulance.Abonement, listKomponen);
        return Task.FromResult(response);
    }
}