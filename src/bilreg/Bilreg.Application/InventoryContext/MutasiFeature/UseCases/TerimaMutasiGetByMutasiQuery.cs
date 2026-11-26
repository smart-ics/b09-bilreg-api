using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record TerimaMutasiGetByMutasiQuery(string MutasiBrgId)
    : IRequest<TerimaMutasiGetResponse>, IMutasiBrgKey;

public class TerimaMutasiGetByMutasiHandler : IRequestHandler<TerimaMutasiGetByMutasiQuery, TerimaMutasiGetResponse>
{
    private readonly ITerimaMutasiRepo _terimaMutasiRepo;

    public TerimaMutasiGetByMutasiHandler(ITerimaMutasiRepo terimaMutasiRepo)
    {
        _terimaMutasiRepo = terimaMutasiRepo;
    }

    public Task<TerimaMutasiGetResponse> Handle(TerimaMutasiGetByMutasiQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiBrgId, nameof(request.MutasiBrgId));

        var model = _terimaMutasiRepo.LoadByMutasiBrgId(request.MutasiBrgId.Trim())
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"TerimaMutasi for MutasiBrg '{request.MutasiBrgId}' not found")
            );

        return Task.FromResult(TerimaMutasiGetResponse.FromModel(model));
    }
}
