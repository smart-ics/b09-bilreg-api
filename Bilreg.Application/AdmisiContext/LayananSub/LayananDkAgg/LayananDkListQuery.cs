using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananDkAgg
{
    public record LayananDkListQuery() : IRequest<IEnumerable<LayananDkListQueryResponse>>;

    public record LayananDkListQueryResponse(
        string LayananDkId,
        string LayananDkName,
        decimal RawatInapCode,
        decimal RawatJalanCode,
        decimal KesehatanJiwaCode,
        decimal BedahCode,
        decimal RujukanCode,
        decimal KunjunganRumahCode,
        decimal LayananSubCode
        );

    public class LayananDkListQueryHandler : IRequestHandler<LayananDkListQuery, IEnumerable<LayananDkListQueryResponse>>
    {
        private readonly ILayananDkDal _layananDkDal;

        public LayananDkListQueryHandler(ILayananDkDal layananDkDal)
        {
            _layananDkDal = layananDkDal;
        }

        public Task<IEnumerable<LayananDkListQueryResponse>> Handle(LayananDkListQuery request, CancellationToken cancellationToken)
        {
            var result = _layananDkDal.ListData()
                ?? throw new KeyNotFoundException($"LayananDk with id not found.");

            var response = result.Select(x => new LayananDkListQueryResponse(
                x.LayananDkId,
                x.LayananDkName,
                x.RawatInapCode,
                x.RawatJalanCode,
                x.KesehatanJiwaCode,
                x.BedahCode,
                x.RujukanCode,
                x.KunjunganRumahCode,
                x.LayananSubCode
                ));

            return Task.FromResult(response);
        }

    }
}
