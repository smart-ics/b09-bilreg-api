using MediatR;

namespace Farinv.Application.BrgContext.StandardFeature.UseCases;

public record FormulariumListQuery() : IRequest<IEnumerable<FormulariumListResponse>>;

public record FormulariumListResponse(string FormulariumId, string FormulariumName);

public class FormulariumListHandler : IRequestHandler<FormulariumListQuery, IEnumerable<FormulariumListResponse>>
{
    private readonly IFormulariumRepo _repo;

    public FormulariumListHandler(IFormulariumRepo repo)
    {
        _repo = repo; 
    }

    public Task<IEnumerable<FormulariumListResponse>> Handle(FormulariumListQuery request, CancellationToken cancellationToken)
    {
        var listFormularium = _repo.ListData()?.ToList() ?? [];
        var response = listFormularium
            .OrderBy(x => x.FormulariumName)
            .Select(x => new FormulariumListResponse(x.FormulariumId, x.FormulariumName));

        return Task.FromResult(response);
    }
}