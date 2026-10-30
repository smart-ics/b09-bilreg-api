using Ardalis.GuardClauses;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

public record LabTestDefinitionGetQuery(string TestDefinitionId)
    : IRequest<LabTestDefinitionDetailResponse>, ILabTestDefinitionKey;

public class LabTestDefinitionGetHandler
    : IRequestHandler<LabTestDefinitionGetQuery, LabTestDefinitionDetailResponse>
{
    private readonly ILabTestDefinitionRepo _repo;
    private readonly INilaiTarifRepo? _nilaiTarifRepo;

    public LabTestDefinitionGetHandler(
        ILabTestDefinitionRepo repo,
        INilaiTarifRepo? nilaiTarifRepo = null)
    {
        _repo = repo;
        _nilaiTarifRepo = nilaiTarifRepo;
    }

    public Task<LabTestDefinitionDetailResponse> Handle(
        LabTestDefinitionGetQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TestDefinitionId, nameof(request.TestDefinitionId));

        var model = _repo.LoadEntity(request)
            .GetValueOrThrow($"LabTestDefinition '{request.TestDefinitionId}' not found");

        var tarifPrice = LabTestDefinitionSupport.ResolveTarifPrice(_nilaiTarifRepo, model.TarifId);
        return Task.FromResult(LabTestDefinitionResponseMapper.ToDetailResponse(model, tarifPrice));
    }
}
