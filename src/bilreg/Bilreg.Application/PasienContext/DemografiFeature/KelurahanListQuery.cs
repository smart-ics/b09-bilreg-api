using MediatR;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public record KelurahanListQuery(string Keyword) : IRequest<IEnumerable<KelurahanView>>;
public class KelurahanListHandler : IRequestHandler<KelurahanListQuery, IEnumerable<KelurahanView>>
{
    private readonly IKelurahanRepo _kelurahanRepo;

    public KelurahanListHandler(IKelurahanRepo kelurahanRepo)
    {
        _kelurahanRepo = kelurahanRepo;
    }

    public Task<IEnumerable<KelurahanView>> Handle(KelurahanListQuery request,
        CancellationToken cancellationToken)
    {
        var result = _kelurahanRepo.ListData(request.Keyword);
        return Task.FromResult(result);
    }
}