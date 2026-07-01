using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.PaymentContext.RegOutFeature.UseCase;

public record RegSearchRegQuery(string Keyword) : IRequest<IEnumerable<RegSearchRegView>>;

public class RegSearchRegHandler : IRequestHandler<RegSearchRegQuery, IEnumerable<RegSearchRegView>>
{
    private readonly IRegAktifRepo _regAktifRepo;
    private const int LIMIT_CONTER = 200;
    public RegSearchRegHandler(IRegAktifRepo regAktifRepo)
        => _regAktifRepo = regAktifRepo;

    public Task<IEnumerable<RegSearchRegView>> Handle(RegSearchRegQuery request, CancellationToken cancellationToken)
    {
        var result = _regAktifRepo.ListData(request.Keyword)?.ToList() ?? [];
        if (result.Count > LIMIT_CONTER)
            throw new TooManyResultsException(LIMIT_CONTER, "Gunakan keyword search lebih spesifik");

        return Task.FromResult(result.AsEnumerable());
    }
}