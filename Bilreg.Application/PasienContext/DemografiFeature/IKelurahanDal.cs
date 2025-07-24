using Bilreg.Domain.PasienContext.DemografiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public interface IKelurahanDal :
    IInsert<KelurahanType>,
    IUpdate<KelurahanType>,
    IDelete<IKelurahanKey>,
    IGetDataMayBe<KelurahanType, IKelurahanKey>,
    IListDataMayBe<KelurahanType, IKecamatanKey>
{
}