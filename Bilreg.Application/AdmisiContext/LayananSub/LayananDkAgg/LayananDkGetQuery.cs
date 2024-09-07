using Bilreg.Domain.AdmisiContext.LayananSub.LayananDkAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananDkAgg
{
    public record LayananDkGetQuery(string LayananDkId):IRequest<LayananDkGetQueryResponse>,ILayananDkKey;

    public record LayananDkGetQueryResponse(
     string  LayananDkId,
     string  LayananDkName,
     decimal RawatInapCode,
     decimal RawatJalanCode,
     decimal KesehatanJiwaCode,
     decimal BedahCode,
     decimal RujukanCode,
     decimal KunjunganRumahCode,
     decimal LayananSubCode
    );

    public class LayananDkGetQueryHandler : IRequestHandler<LayananDkGetQuery, LayananDkGetQueryResponse>
    {
        private readonly ILayananDkDal _layananDkDal;

        public LayananDkGetQueryHandler(ILayananDkDal layananDkDal)
        {
            _layananDkDal = layananDkDal;
        }

        public Task<LayananDkGetQueryResponse> Handle(LayananDkGetQuery request, CancellationToken cancellationToken)
        {

            var result = _layananDkDal.GetData(request)
                ?? throw new KeyNotFoundException("LayananDk with id not found");

            var response = new LayananDkGetQueryResponse(
                result.LayananDkId,
                result.LayananDkName,
                result.RawatInapCode,
                result.RawatJalanCode,
                result.KesehatanJiwaCode,
                result.BedahCode,
                result.RujukanCode,
                result.KunjunganRumahCode,
                result.LayananSubCode
            );

            return Task.FromResult(response);
        }
    }
}
