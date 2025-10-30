using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSearchQuery(string Keyword) :IRequest<IEnumerable<PasienPersonView>> ;

public class PasienSearchHandler : IRequestHandler<PasienSearchQuery, IEnumerable<PasienPersonView>>
{
    private readonly IPasienRepo _repo;

    public PasienSearchHandler(IPasienRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<PasienPersonView>> Handle(PasienSearchQuery request, CancellationToken cancellationToken)
    {

        var result = _repo.ListData(request.Keyword);
        return Task.FromResult(result);
    }
}
