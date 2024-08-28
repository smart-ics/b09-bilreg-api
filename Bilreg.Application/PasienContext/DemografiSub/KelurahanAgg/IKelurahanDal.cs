using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;

public interface IKelurahanDal :
    IInsert<KelurahanModel>,
    IUpdate<KelurahanModel>,
    IDelete<IKelurahanKey>,
    IGetData<KelurahanModel, IKelurahanKey>,
    IListData<KelurahanModel, IKecamatanKey>,
    IListData<KelurahanModel, string>
{
}