using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;

public interface IKecamatanDal: 
    IInsert<KecamatanModel>,
    IUpdate<KecamatanModel>,
    IDelete<IKecamatanKey>,
    IGetData<KecamatanModel, IKecamatanKey>,
    IListData<KecamatanModel, IKabupatenKey>
{
    
}