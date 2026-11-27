using Ardalis.GuardClauses;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record ListTransportByRegQuery(string RegId) : IRequest<IEnumerable<TransportGetResponse>>, IRegKey;

public class ListTransportByRegHandler : IRequestHandler<ListTransportByRegQuery, IEnumerable<TransportGetResponse>>
{
    private readonly ITrsTransportRepo _trsTransportRepo;
    private readonly IKomponenRepo _komponenRepo;

    public ListTransportByRegHandler(
        ITrsTransportRepo trsTransportRepo,
        IKomponenRepo komponenRepo)
    {
        _trsTransportRepo = trsTransportRepo;
        _komponenRepo = komponenRepo;
    }

    public Task<IEnumerable<TransportGetResponse>> Handle(ListTransportByRegQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var list = (_trsTransportRepo.ListDataByReg(request.RegId) ?? []).ToList();
        var kompKeys = list
            .SelectMany(t => t.ListItems)
            .Select(i => i.DetilTarifId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .Select(id => (IKomponenKey)KomponenType.Key(id))
            .ToList();

        var komponenNames = kompKeys.Count > 0
            ? (_komponenRepo.ListData(kompKeys) ?? []).ToDictionary(k => k.KomponenId, k => k.KomponenName)
            : new Dictionary<string, string>();

        var response = list.Select(t => TransportResponseMapper.ToResponse(t, komponenNames)).ToList();

        return Task.FromResult<IEnumerable<TransportGetResponse>>(response);
    }
}
