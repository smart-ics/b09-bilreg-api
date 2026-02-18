using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.PaymentContext.RegOutFeature.UseCase;

public record RegSearchRegQuery(string Keyword) : IRequest<IEnumerable<RegSearchRegView>>;

public class RegSearchRegHandler : IRequestHandler<RegSearchRegQuery, IEnumerable<RegSearchRegView>>
{
    private readonly IRegRepo _regRepo;
    private const int LIMIT_CONTER = 200;
    public RegSearchRegHandler(IRegRepo regRepo)
        => _regRepo = regRepo;

    public Task<IEnumerable<RegSearchRegView>> Handle(RegSearchRegQuery request, CancellationToken cancellationToken)
    {
        var result = _regRepo.ListData(request.Keyword)?.ToList() ?? [];
        if (result.Count > LIMIT_CONTER)
            throw new TooManyResultsException(LIMIT_CONTER, "Gunakan keyword search lebih spesifik");

        return Task.FromResult(result.AsEnumerable());
    }
}