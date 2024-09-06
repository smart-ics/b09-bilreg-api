using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg
{
    public record GrupKomponenGetQuery(string GrupKomponenId):IRequest<GrupKomponenGetResponse>,IGrupKomponenKey;
    public record GrupKomponenGetResponse(
        string GrupKomponenId,
        string GrupKomponenName
    );
    public class GrupKomponenGetQueryHandler : IRequestHandler<GrupKomponenGetQuery,GrupKomponenGetResponse>
    {
        private readonly IGrupKomponenDal _grupKomponenDal;

        public GrupKomponenGetQueryHandler(IGrupKomponenDal grupKomponenDal)
        {
            _grupKomponenDal = grupKomponenDal;
        }

        public Task<GrupKomponenGetResponse> Handle(GrupKomponenGetQuery request, CancellationToken cancellationToken)
        {
            var result = _grupKomponenDal.GetData(request)
                 ?? throw new KeyNotFoundException($"Grup Komponen with id{request.GrupKomponenId} not found.");

            var response = new GrupKomponenGetResponse(
                result.GrupKomponenId, result.GrupKomponenName
                );
            return Task.FromResult(response);
        }
    }


}
