using Bilreg.Domain.LabContext.LabMasterFeature;
using MediatR;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public record LabDisciplineListResponse(
    string DisciplineId,
    string DisciplineName);

public record LabDisciplineListQuery(string? Search = null)
    : IRequest<IEnumerable<LabDisciplineListResponse>>;

public class LabDisciplineListHandler
    : IRequestHandler<LabDisciplineListQuery, IEnumerable<LabDisciplineListResponse>>
{
    private readonly ILabDisciplineRepo _repo;

    public LabDisciplineListHandler(ILabDisciplineRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<LabDisciplineListResponse>> Handle(
        LabDisciplineListQuery request,
        CancellationToken cancellationToken)
    {
        var disciplines = string.IsNullOrWhiteSpace(request.Search)
            ? _repo.ListData()
            : _repo.ListData(request.Search);

        var response = disciplines
            .Select(x => new LabDisciplineListResponse(x.DisciplineId, x.DisciplineName));

        return Task.FromResult(response);
    }
}
