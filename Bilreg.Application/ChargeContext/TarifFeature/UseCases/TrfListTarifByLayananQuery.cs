using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfListTarifByLayananQuery(string LayananId) : IRequest<IEnumerable<TrfListTarifByLayananReesponse>>;

public record TrfListTarifByLayananReesponse(string TarifId, string TarifName, decimal NilaiTarif);

public class TrfListTarifByLayananQueryHandler : IRequestHandler<TrfListTarifByLayananQuery, IEnumerable<TrfListTarifByLayananReesponse>>
{
    //private readonly ITarifRepo _tarifRepo;
    //public TrfListTarifByLayananQueryHandler(ITarifRepo tarifRepo)
    //{
    //    _tarifRepo = tarifRepo;
    //}
    public Task<IEnumerable<TrfListTarifByLayananReesponse>> Handle(TrfListTarifByLayananQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}


