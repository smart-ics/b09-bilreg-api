using Bilreg.Application.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSearchQuery(string Keyword) :IRequest<IEnumerable<PasienPersonView>> ;

public class PasienSearchHandler : IRequestHandler<PasienSearchQuery, IEnumerable<PasienPersonView>>
{
    private readonly IPasienRepo _repo;
    private const int LIMIT_CONTER = 200;

    public PasienSearchHandler(IPasienRepo repo) 
        => _repo = repo;

    public Task<IEnumerable<PasienPersonView>> Handle(PasienSearchQuery request, CancellationToken cancellationToken)
    {
        var result = _repo.SearchPasien(request.Keyword)?.ToList() ?? [];
        if (result.Count > LIMIT_CONTER)
            throw new TooManyResultsException(LIMIT_CONTER, "Gunakan keyword search lebih spesifik");
        
        return Task.FromResult(result.AsEnumerable());
    }
}
