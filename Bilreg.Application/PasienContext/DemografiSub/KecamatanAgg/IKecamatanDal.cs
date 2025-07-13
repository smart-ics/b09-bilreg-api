using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;

public interface IKecamatanDal: 
    IInsert<KecamatanType>,
    IUpdate<KecamatanType>,
    IDelete<IKecamatanKey>,
    IGetData<KecamatanType, IKecamatanKey>,
    IListData<KecamatanType, IKabupatenKey>
{
    
}