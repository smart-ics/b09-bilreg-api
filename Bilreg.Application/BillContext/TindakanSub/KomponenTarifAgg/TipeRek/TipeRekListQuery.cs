using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRek
{
    public record TipeRekListQuery() : IRequest<IEnumerable<TipeRekListQueryResponse>>;

    public record TipeRekListQueryResponse(
        string TipeRekId,
        string TipeRekName,
        bool IsNeraca,
        decimal NoUrut,
        string DebetKredit);

    public class TipeRekListQueryHandler : IRequestHandler<TipeRekListQuery, IEnumerable<TipeRekListQueryResponse>>
    {
        private readonly ITipeRekDal _tipeRekDal;

        public TipeRekListQueryHandler(ITipeRekDal tipeRekDal)
        {
            _tipeRekDal = tipeRekDal;
        }

        public Task<IEnumerable<TipeRekListQueryResponse>> Handle(TipeRekListQuery request, CancellationToken cancellationToken)
        {
            var result = _tipeRekDal.ListData()
                ?? throw new KeyNotFoundException($"Tipe Rek with id not found.");

            var response = result.Select(x => new TipeRekListQueryResponse(
                x.TipeRekId, x.TipeRekName, x.IsNeraca, x.NoUrut, x.DebetKredit
                ));

            return Task.FromResult(response);
        }

    }
}
