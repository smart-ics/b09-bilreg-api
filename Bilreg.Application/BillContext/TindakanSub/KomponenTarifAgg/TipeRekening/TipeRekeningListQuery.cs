using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRekening
{
    public record TipeRekeningListQuery() : IRequest<IEnumerable<TipeRekeningListQueryResponse>>;

    public record TipeRekeningListQueryResponse(
        string TipeRekeningId,
        string TipeRekeningName,
        bool IsNeraca,
        decimal NoUrut,
        string DebetKredit);

    public class TipeRekeningListQueryHandler : IRequestHandler<TipeRekeningListQuery, IEnumerable<TipeRekeningListQueryResponse>>
    {
        private readonly ITipeRekeningDal _tipeRekeningDal;

        public TipeRekeningListQueryHandler(ITipeRekeningDal tipeRekeningDal)
        {
            _tipeRekeningDal = tipeRekeningDal;
        }

        public Task<IEnumerable<TipeRekeningListQueryResponse>> Handle(TipeRekeningListQuery request, CancellationToken cancellationToken)
        {
            var result = _tipeRekeningDal.ListData()
                ?? throw new KeyNotFoundException($"Tipe Rekening with id not found.");

            var response = result.Select(x => new TipeRekeningListQueryResponse(
                x.TipeRekeningId,x.TipeRekeningName,x.IsNeraca,x.NoUrut,x.DebetKredit
                ));

            return Task.FromResult(response);
        }

    }
}
