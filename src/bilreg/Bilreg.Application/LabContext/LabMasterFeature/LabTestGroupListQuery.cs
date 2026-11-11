using Bilreg.Domain.LabContext.LabMasterFeature;
using MediatR;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public record LabTestGroupListResponse(
    string TestGroupId,
    string TestGroupName);

public record LabTestGroupListQuery(string? Search = null)
    : IRequest<IEnumerable<LabTestGroupListResponse>>;

public class LabTestGroupListHandler
    : IRequestHandler<LabTestGroupListQuery, IEnumerable<LabTestGroupListResponse>>
{
    private readonly ILabTestGroupRepo _repo;

    public LabTestGroupListHandler(ILabTestGroupRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<LabTestGroupListResponse>> Handle(
        LabTestGroupListQuery request,
        CancellationToken cancellationToken)
    {
        var testGroups = string.IsNullOrWhiteSpace(request.Search)
            ? _repo.ListData()
            : _repo.ListData(request.Search);

        var response = testGroups
            .Select(x => new LabTestGroupListResponse(x.TestGroupId, x.TestGroupName));

        return Task.FromResult(response);
    }
}
