using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public interface IPasienDal : 
    IInsert<PasienModel>,
    IUpdate<PasienModel>,
    IDelete<PasienModel>,
    IGetData2<PasienModel, IPasienKey>,
    IListData2<PasienModel, DateTime>,
    IListData2<PasienModel, Periode>,
    IListData2<PasienModel,string>
{
}