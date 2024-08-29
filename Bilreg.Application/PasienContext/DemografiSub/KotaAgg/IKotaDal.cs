using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;

public interface IKotaDal:
    IInsert<KotaModel>,
    IUpdate<KotaModel>,
    IDelete<IKotaKey>,
    IGetData<KotaModel, IKotaKey>,
    IListData<KotaModel>
{
    
}