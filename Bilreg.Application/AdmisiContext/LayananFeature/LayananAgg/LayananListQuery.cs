using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananFeature.LayananAgg
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
        private readonly ILayananRepo _layananRepo;

        public LayananListHandler(ILayananRepo layananRepo)
        {
            _layananRepo = layananRepo;
        }

        public Task<IEnumerable<LayananListResponse>> Handle(LayananListQuery request,
            CancellationToken cancellationToken)
        {
            var listLyn = _layananRepo.ListData()?.ToList() ?? [];
            var response = listLyn
                .Select(x => new LayananListResponse(x.LayananId, x.LayananName,
                    x.IsAktif, x.Instalasi.InstalasiId, x.Instalasi.InstalasiName));
            return Task.FromResult(response);
        }
    }
}
