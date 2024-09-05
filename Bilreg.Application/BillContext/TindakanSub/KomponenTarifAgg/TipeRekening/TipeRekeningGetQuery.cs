using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRekening
{
    public record TipeRekeningGetQuery(string TipeRekeningId) : IRequest<TipeRekeningGetResponse>, ITipeRekeningKey;
    public class TipeRekeningGetResponse(
        string TipeRekeningId,
        string TipeRekeningName,
        bool IsNeraca,
        decimal NoUrut,
        string DebetKredit
    );
    public class TipeRekeningGetQueryHandler : IRequestHandler<TipeRekeningGetQuery, TipeRekeningGetResponse>
    {
        private readonly ITipeRekeningDal _tipeRekeningDal;

        public TipeRekeningGetQueryHandler(ITipeRekeningDal tipeRekeningDal)
        {
            _tipeRekeningDal = tipeRekeningDal;
        }

        public Task<TipeRekeningGetResponse> Handle(TipeRekeningGetQuery request, CancellationToken cancellationToken)
        {
            var result = _tipeRekeningDal.GetData(request)
                 ?? throw new KeyNotFoundException($"Tipe Rekening with id{request.TipeRekeningId} not found.");

            var response = new TipeRekeningGetResponse(
                result.TipeRekeningId, result.TipeRekeningName, result.IsNeraca, result.NoUrut, result.DebetKredit
                );

            return Task.FromResult(response);
        }
    }
}
