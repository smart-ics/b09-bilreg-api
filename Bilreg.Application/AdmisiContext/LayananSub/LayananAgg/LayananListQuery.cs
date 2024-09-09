using Castle.Components.DictionaryAdapter.Xml;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananListQuery():IRequest<IEnumerable<LayananListQueryResponse>>;

    public record LayananListQueryResponse(
        string LayananId,
        string LayananName,
        bool IsAktif,
        string InstalasiId,
        string InstalasiName,
        string LayananDkId,
        string LayananDkName,
        string LayananTipeDkId,
        string LayananTipeDkName,
        string SmfId,
        string SmfName,
        string PetugasMedisId,
        string PetugasMedisName
        );
    public class LayananListQueryHandler : IRequestHandler<LayananListQuery, IEnumerable<LayananListQueryResponse>>
    {
        private readonly ILayananDal _layananDal;

        public LayananListQueryHandler(ILayananDal layananDal)
        {
            _layananDal = layananDal;
        }

        public Task<IEnumerable<LayananListQueryResponse>> Handle(LayananListQuery request, CancellationToken cancellationToken)
        {
            var list = _layananDal.ListData()
                ?? throw new KeyNotFoundException("Layanan not found");
            return Task.FromResult(list.Select(x => new LayananListQueryResponse(
                x.LayananId,
                x.LayananName,
                x.IsAktif,
                x.InstalasiId,
                x.InstalasiName,
                x.LayananDkId,
                x.LayananDkName,
                x.LayananTipeDkId,
                x.LayananTipeDkName,
                x.SmfId,
                x.SmfName,
                x.PetugasMedisId,
                x.PetugasMedisName
                )));
        }
    }
}
