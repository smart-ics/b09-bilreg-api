using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record AdmissionServicePointListQry(bool ActiveOnly = true)
    : IRequest<IReadOnlyList<AdmissionServicePointListResponse>>;

public record AdmissionServicePointListResponse(
    string ServicePointId,
    string DisplayName,
    string QueuePrefix,
    string Status);

public sealed class AdmissionServicePointListHandler
    : IRequestHandler<AdmissionServicePointListQry, IReadOnlyList<AdmissionServicePointListResponse>>
{
    private readonly IAdmissionServicePointRepo _repo;

    public AdmissionServicePointListHandler(IAdmissionServicePointRepo repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<AdmissionServicePointListResponse>> Handle(
        AdmissionServicePointListQry request,
        CancellationToken cancellationToken)
    {
        var result = _repo.ListAll()
            .Where(x => !request.ActiveOnly || x.IsActive)
            .Select(ToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<AdmissionServicePointListResponse>>(result);
    }

    private static AdmissionServicePointListResponse ToResponse(AdmissionServicePointModel model) =>
        new(model.ServicePointId, model.DisplayName, model.QueuePrefix, model.Status.ToString());
}
