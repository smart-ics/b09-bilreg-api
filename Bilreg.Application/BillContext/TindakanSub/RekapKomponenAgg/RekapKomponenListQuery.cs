using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.RekapKomponenAgg
{
    public record RekapKomponenListQuery():IRequest<IEnumerable<RekapKomponenListQueryResponse>>;
    public record RekapKomponenListQueryResponse(string RekapKomponenId,string RekapKomponenName, decimal RekapKomponenUrut);
    public class RekapKomponenListQueryHandler : IRequestHandler<RekapKomponenListQuery, IEnumerable<RekapKomponenListQueryResponse>>
    {
        private readonly IRekapKomponenDal _rekapKomponenDal;
        public RekapKomponenListQueryHandler(IRekapKomponenDal rekapKomponenDal)
        {
            _rekapKomponenDal = rekapKomponenDal;
        }
        public Task<IEnumerable<RekapKomponenListQueryResponse>> Handle(RekapKomponenListQuery request, CancellationToken cancellationToken)
        {
            var result = _rekapKomponenDal.ListData()
                ?? throw new KeyNotFoundException($"Rekap Komponen with id not found.");

            var response = result.Select(x => new RekapKomponenListQueryResponse(x.RekapKomponenId, x.RekapKomponenName, x.RekapKomponenUrut));
            
            return Task.FromResult(response);
        }
    }

}
