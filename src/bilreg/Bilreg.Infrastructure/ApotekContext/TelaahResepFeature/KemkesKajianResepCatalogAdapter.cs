using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;

namespace Bilreg.Infrastructure.ApotekContext.TelaahResepFeature;

public class KemkesKajianResepCatalogAdapter : IKemkesKajianResepCatalogPort
{
    private readonly IKemkesKajianResepDal _dal;

    public KemkesKajianResepCatalogAdapter(IKemkesKajianResepDal dal)
    {
        _dal = dal ?? throw new ArgumentNullException(nameof(dal));
    }

    public IReadOnlyList<TelaahResepKajianModel> GetAll()
    {
        var dtos = _dal.ListData()?.ToList() ?? new List<KemkesKajianDto>();
        if (dtos.Count == 0)
        {
            return TelaahResepKajianModel.DefaultCatalog();
        }

        return dtos
            .Select((x, index) => TelaahResepKajianModel.Create(
                index + 1,
                x.KajianResepId,
                x.KajianResepName,
                x.GrupKajianResepId,
                x.GrupKajianResepName))
            .ToList();
    }
}
