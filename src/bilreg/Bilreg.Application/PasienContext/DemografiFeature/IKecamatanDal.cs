using Bilreg.Domain.PasienContext.DemografiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public interface IKecamatanDal :
    IInsert<KecamatanType>,
    IUpdate<KecamatanType>,
    IDelete<IKecamatanKey>,
    IGetDataMayBe<KecamatanType, IKecamatanKey>,
    IListDataMayBe<KecamatanType, IKabupatenKey>
{
}