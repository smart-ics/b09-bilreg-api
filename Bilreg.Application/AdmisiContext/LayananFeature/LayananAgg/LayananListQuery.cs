using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananListQuery() : IRequest<IEnumerable<LayananListResponse>>;

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
                => _layananDal.ListData()
                    .Match(
                        onSome: x => Task.FromResult(x.Select(y
                            => new LayananListResponse(y.LayananId, y.LayananName,
                                y.IsAktif, y.Instalasi.InstalasiId, y.Instalasi.InstalasiName))),
                        onNone: () => throw new KeyNotFoundException("data not found"));
        
    }
}
