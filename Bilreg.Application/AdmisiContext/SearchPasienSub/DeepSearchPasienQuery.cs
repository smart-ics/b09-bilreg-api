using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public record DeepSearchPasienQuery(string Keyword) : IRequest<IEnumerable<SearchPasienModel>>;

public class DeepSeachPasienHandler : IRequestHandler<DeepSearchPasienQuery, IEnumerable<SearchPasienModel>>
{
    private readonly IDeepSearchPasienDal _deepSearchDal;

    public DeepSeachPasienHandler(IDeepSearchPasienDal deepSearchDal)
    {
        _deepSearchDal = deepSearchDal;
    }

    public Task<IEnumerable<SearchPasienModel>> Handle(DeepSearchPasienQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 2)
            throw new ArgumentException("Keyword terlalu pendek (minimal 2 karakter).");

        var keyword = request.Keyword.Trim();
        var result = _deepSearchDal.ListData(request.Keyword)
            .Match(
                some => some,
                () => throw new KeyNotFoundException("data not found")
            );

        return Task.FromResult(result.Distinct());
    }
}
