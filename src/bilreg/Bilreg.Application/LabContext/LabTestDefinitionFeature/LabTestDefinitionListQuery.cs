using Bilreg.Application.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionListQuery(
    bool ActiveOnly = false,
    string? Search = null,
    string? TarifId = null)
    : IRequest<IEnumerable<LabTestDefinitionListResponse>>;

public class LabTestDefinitionListHandler
    : IRequestHandler<LabTestDefinitionListQuery, IEnumerable<LabTestDefinitionListResponse>>
{
    private readonly ILabTestDefinitionRepo _repo;

    public LabTestDefinitionListHandler(
        ILabTestDefinitionRepo repo,
        INilaiTarifRepo? _ = null)
    {
        _repo = repo;
    }

    public Task<IEnumerable<LabTestDefinitionListResponse>> Handle(
        LabTestDefinitionListQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new LabTestDefinitionListFilter(request.ActiveOnly, request.Search, request.TarifId);
        var response = _repo.ListProjected(filter);
        return Task.FromResult(response);
    }
}
