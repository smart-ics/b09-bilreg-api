using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg.TipeRek;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRek
{
    public record TipeRekGetQuery(string TipeRekId) : IRequest<TipeRekGetResponse>,ITipeRekKey;
    public record TipeRekGetResponse(
       string TipeRekId, string TipeRekName, bool IsNeraca, decimal NoUrut, string DebetKredit
        );
    public class TipeRekGetQueryHandler : IRequestHandler<TipeRekGetQuery, TipeRekGetResponse>
    {
        private readonly ITipeRekDal _tipeRekDal;

        public TipeRekGetQueryHandler(ITipeRekDal tipeRekDal)
        {
            _tipeRekDal = tipeRekDal;
        }

        public Task<TipeRekGetResponse> Handle(TipeRekGetQuery request, CancellationToken cancellationToken)
        {
            var result = _tipeRekDal.GetData(request)
                ?? throw new KeyNotFoundException("Data Tipe Rek Tidak Ditemukan");
            var response = new TipeRekGetResponse(
                result.TipeRekId, result.TipeRekName, result.IsNeraca, 
                result.NoUrut, result.DebetKredit );
            return Task.FromResult(response);
        }
    }
}
