using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg
{
    public record GrupKomponenListQuery(): IRequest<IEnumerable<GrupKomponenListQueryResponse>>;

    public record GrupKomponenListQueryResponse(string GrupKomponenId, string GrupKomponenName);

    public class GrupKomponenListQueryHandler : IRequestHandler<GrupKomponenListQuery, IEnumerable<GrupKomponenListQueryResponse>>
    {
        private readonly IGrupKomponenDal _grupKomponenDal;

        public GrupKomponenListQueryHandler(IGrupKomponenDal grupKomponenDal)
        {
            _grupKomponenDal = grupKomponenDal;
        }

        public Task<IEnumerable<GrupKomponenListQueryResponse>> Handle(GrupKomponenListQuery request, CancellationToken cancellationToken)
        {
            var result = _grupKomponenDal.ListData()
                ?? throw new KeyNotFoundException($"Grup Komponen with id not found.");

            var response = result.Select(x => new GrupKomponenListQueryResponse(x.GrupKomponenId, x.GrupKomponenName));

            return Task.FromResult(response);
        }
    }
}
