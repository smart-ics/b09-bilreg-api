using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadEquipmentLookupQuery(string? Modality = null, bool ActiveOnly = true)
    : IRequest<IEnumerable<RadEquipmentDto>>;

public class RadEquipmentLookupHandler : IRequestHandler<RadEquipmentLookupQuery, IEnumerable<RadEquipmentDto>>
{
    private readonly IRadEquipmentDal _equipmentDal;

    public RadEquipmentLookupHandler(IRadEquipmentDal equipmentDal)
    {
        _equipmentDal = equipmentDal;
    }

    public Task<IEnumerable<RadEquipmentDto>> Handle(
        RadEquipmentLookupQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        IEnumerable<RadEquipmentDto> result;
        if (!string.IsNullOrWhiteSpace(request.Modality))
        {
            result = _equipmentDal.ListByModality(request.Modality.Trim(), request.ActiveOnly);
        }
        else
        {
            result = request.ActiveOnly
                ? _equipmentDal.ListActive()
                : _equipmentDal.ListAll();
        }

        return Task.FromResult(result);
    }
}
