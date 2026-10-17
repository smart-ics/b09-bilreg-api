using MediatR;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;

public record LokasiBerkasF12Query : IRequest<IEnumerable<LokasiBerkasDto>>;

public class LokasiBerkasF12Handler : IRequestHandler<LokasiBerkasF12Query, IEnumerable<LokasiBerkasDto>>
{
    private readonly ILokasiBerkasDal _lokasiDal;

    public LokasiBerkasF12Handler(ILokasiBerkasDal lokasiDal)
    {
        _lokasiDal = lokasiDal;
    }

    public Task<IEnumerable<LokasiBerkasDto>> Handle(LokasiBerkasF12Query request, CancellationToken cancellationToken)
    {
        var result = _lokasiDal.ListAll() ?? [];
        return Task.FromResult(result);
    }
}
