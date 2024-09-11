using Castle.Components.DictionaryAdapter.Xml;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananListQuery():IRequest<IEnumerable<LayananListResponse>>;

    public record LayananListResponse(
        string LayananId,
        string LayananName,
        bool IsAktif,
        string InstalasiId,
        string InstalasiName
        );
    public class LayananListHandler : IRequestHandler<LayananListQuery, IEnumerable<LayananListResponse>>
    {
        private readonly ILayananDal _layananDal;

        public LayananListHandler(ILayananDal layananDal)
        {
            _layananDal = layananDal;
        }

        public Task<IEnumerable<LayananListResponse>> Handle(LayananListQuery request, CancellationToken cancellationToken)
        {
            var list = _layananDal.ListData()
                ?? throw new KeyNotFoundException("Layanan not found");
            return Task.FromResult(list.Select(x => new LayananListResponse(
                x.LayananId,
                x.LayananName,
                x.IsAktif,
                x.InstalasiId,
                x.InstalasiName
                )));
        }
    }
}
