using Bilreg.Domain.BillContext.TindakanSub.GrupTarifDkAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.GrupTarifDkAgg
{
    public record GrupTarifDkGetQuery(string GrupTarifDkId) : IRequest<GrupTarifDkResponse>, IGrupTarifDkKey;
    public record GrupTarifDkResponse(
        string GrupTarifDkId,
        string GrupTarifDkName
    );

    public class GrupTarifDkGetHandler : IRequestHandler<GrupTarifDkGetQuery, GrupTarifDkResponse>
    {
        private IGrupTarifDkDal _grupTarifDkDal;

        public GrupTarifDkGetHandler(IGrupTarifDkDal grupTarifDkDal)
        {
            _grupTarifDkDal = grupTarifDkDal;
        }

        public Task<GrupTarifDkResponse> Handle(GrupTarifDkGetQuery request, CancellationToken cancellationToken)
        {
            var result = _grupTarifDkDal.GetData(request)
                ??  throw new KeyNotFoundException($"GrupTarifDkId id :{request.GrupTarifDkId} not found");

            var response = new GrupTarifDkResponse(
                result.GrupTarifDkId, result.GrupTarifDkName
            );
            return Task.FromResult(response);
        }
    }
}

