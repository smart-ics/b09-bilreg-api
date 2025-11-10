using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegGetQuery(string RegId) : IRequest<RegModel>, IRegKey;

public class RegJalanGethandler : IRequestHandler<RegGetQuery, RegModel>
{
    private readonly IRegRepo _regRepo;
    public RegJalanGethandler(IRegRepo regRepo)
    {
        _regRepo = regRepo;
    }

    public Task<RegModel> Handle(RegGetQuery request, CancellationToken cancellationToken)
        => _regRepo.LoadEntity(request)
        .Match(
                onSome: x => Task.FromResult(x),
                onNone: () => throw new KeyNotFoundException($"Register {request.RegId} not found")
        );
}
