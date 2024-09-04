using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.RekapKomponenAgg
{
    public record RekapKomponenGetQuery(string RekapKomponenId):IRequest,IRekapKomponenKey;
    public record RekapKomponenGetResponse(string RekapKomponenId,string RekapKomponenName, decimal RekapKomponenUrut);
    public class RekapKomponenGetQueryHandler : IRequestHandler<RekapKomponenGetQuery>
    {
        private readonly IRekapKomponenDal _rekapKomponenDal;

        public RekapKomponenGetQueryHandler(IRekapKomponenDal rekapKomponenDal)
        {
            _rekapKomponenDal = rekapKomponenDal;
        }

        public Task Handle(RekapKomponenGetQuery request, CancellationToken cancellationToken)
        {
            var result = _rekapKomponenDal.GetData(request)
                ?? throw new KeyNotFoundException($"Rekap Komponen with id{request.RekapKomponenId} not found.");

            var response = new RekapKomponenGetResponse(result.RekapKomponenId, result.RekapKomponenName, result.RekapKomponenUrut);
            return Task.FromResult(response);
        }
    }

}
