using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.TelaahResepFeature;

public interface ITelaahResepRepo :
    ISaveChange<TelaahResepModel>,
    ILoadEntity<TelaahResepModel, ITelaahResepKey>
{
    MayBe<TelaahResepModel> LoadByResepKerja(string resepKerjaId);
}
