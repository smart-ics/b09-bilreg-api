using Bilreg.Domain.ApotekContext.TelaahResepFeature;

namespace Bilreg.Application.ApotekContext.TelaahResepFeature;

public interface IKemkesKajianResepCatalogPort
{
    IReadOnlyList<TelaahResepKajianModel> GetAll();
}
