using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record AmbulanceListQuery() : IRequest<IEnumerable<AmbulanceGetResponse>>;

public record ListMasterAmbulanceQuery() : IRequest<IEnumerable<AmbulanceGetResponse>>;

public class AmbulanceListHandler :
    IRequestHandler<AmbulanceListQuery, IEnumerable<AmbulanceGetResponse>>,
    IRequestHandler<ListMasterAmbulanceQuery, IEnumerable<AmbulanceGetResponse>>
{
    private readonly IAmbulanceRepo _ambulanceRepo;

    public AmbulanceListHandler(IAmbulanceRepo ambulanceRepo)
    {
        _ambulanceRepo = ambulanceRepo;
    }

    public Task<IEnumerable<AmbulanceGetResponse>> Handle(AmbulanceListQuery request, CancellationToken cancellationToken)
    {
        return HandleInternal();
    }

    public Task<IEnumerable<AmbulanceGetResponse>> Handle(ListMasterAmbulanceQuery request, CancellationToken cancellationToken)
    {
        return HandleInternal();
    }

    private Task<IEnumerable<AmbulanceGetResponse>> HandleInternal()
    {
        var list = _ambulanceRepo.ListData() ?? [];
        var result = list.Select(ambulance =>
        {
            var listKomponen = ambulance.ListKomponen
                .Select(k => new AmbulanceKomponenResponse(k.KomponenId, k.NilaiTarif, k.IsTetap))
                .ToList();

            var listTujuan = ambulance.ListTujuan
                .Select(t => new AmbulanceTujuanResponse(t.TujuanTransportId, t.Konstanta))
                .ToList();

            return new AmbulanceGetResponse(
                ambulance.AmbulanceId,
                ambulance.AmbulanceName,
                ambulance.IsAktif,
                ambulance.Abonement,
                ambulance.NoPolisi,
                ambulance.TipeArmada,
                ambulance.StatusArmada,
                listKomponen,
                listTujuan);
        }).ToList();

        return Task.FromResult<IEnumerable<AmbulanceGetResponse>>(result);
    }
}
