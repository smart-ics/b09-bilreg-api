using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananFeature
{
    public record LayananListQuery() : IRequest<IEnumerable<LayananListResponse>>;

    public record LayananListResponse(
        string LayananId,
        string LayananName,
        bool IsAktif,
        string InstalasiId,
        string InstalasiName,
        string PoliBpjsId,
        string PoliBpjsName
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
                .OrderBy(x => x.LayananName)
                .Select(x => new LayananListResponse(x.LayananId, x.LayananName,
                    x.IsAktif, x.Instalasi.InstalasiId, x.Instalasi.InstalasiName,
                    x.PoliBpjs.PoliBpjsId, x.PoliBpjs.PoliBpjsName));
            return Task.FromResult(response);
        }
    }
}
