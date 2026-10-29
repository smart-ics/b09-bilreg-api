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
    private readonly INilaiTarifRepo? _nilaiTarifRepo;

    public LabTestDefinitionListHandler(
        ILabTestDefinitionRepo repo,
        INilaiTarifRepo? nilaiTarifRepo = null)
    {
        _repo = repo;
        _nilaiTarifRepo = nilaiTarifRepo;
    }

    public Task<IEnumerable<LabTestDefinitionListResponse>> Handle(
        LabTestDefinitionListQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new LabTestDefinitionListFilter(request.ActiveOnly, request.Search, request.TarifId);
        var definitions = _repo.ListData(filter).ToList();

        var priceCache = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var response = definitions.Select(model =>
        {
            var tarifId = model.TarifId;
            if (!priceCache.TryGetValue(tarifId, out var price))
            {
                price = LabTestDefinitionSupport.ResolveTarifPrice(_nilaiTarifRepo, tarifId);
                priceCache[tarifId] = price;
            }

            return LabTestDefinitionResponseMapper.ToListResponse(model, price);
        });

        return Task.FromResult(response);
    }
}
