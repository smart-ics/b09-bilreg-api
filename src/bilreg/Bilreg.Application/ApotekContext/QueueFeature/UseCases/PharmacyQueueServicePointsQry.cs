using Bilreg.Domain.ApotekContext.QueueFeature;
using MediatR;

namespace Bilreg.Application.ApotekContext.QueueFeature.UseCases;

/// <summary>
/// Query daftar titik layanan (service point) antrean depo farmasi.
/// </summary>
public record PharmacyQueueServicePointsQry(
    string? DepoFarmasiId = null,
    bool ActiveOnly = true) : IRequest<IReadOnlyList<PharmacyServicePointResponse>>;

/// <summary>
/// Handler untuk query daftar service point depo farmasi.
/// </summary>
public class PharmacyQueueServicePointsHandler
    : IRequestHandler<PharmacyQueueServicePointsQry, IReadOnlyList<PharmacyServicePointResponse>>
{
    private readonly IPharmacyServicePointRepo _repo;

    public PharmacyQueueServicePointsHandler(IPharmacyServicePointRepo repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<PharmacyServicePointResponse>> Handle(
        PharmacyQueueServicePointsQry request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PharmacyServicePointModel> list;
        if (!string.IsNullOrWhiteSpace(request.DepoFarmasiId))
        {
            list = _repo.ListByDepo(request.DepoFarmasiId.Trim());
            if (request.ActiveOnly)
            {
                list = list.Where(x => x.Active).ToList();
            }
        }
        else
        {
            list = _repo.ListActive();
        }

        var result = list
            .Select(x => new PharmacyServicePointResponse(
                x.ServicePointId,
                x.DepoFarmasiId,
                x.DisplayName,
                x.QueuePrefix,
                x.Active))
            .ToList();

        return Task.FromResult<IReadOnlyList<PharmacyServicePointResponse>>(result);
    }
}
