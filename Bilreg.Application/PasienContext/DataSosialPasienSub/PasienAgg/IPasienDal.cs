using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public interface IPasienDal : 
    IInsert<PasienModel>,
    IUpdate<PasienModel>,
    IDelete<PasienModel>,
    IGetData<PasienModel, IPasienKey>,
    IListData<PasienModel, DateTime>,
    IListData<PasienModel, Periode>
{
}