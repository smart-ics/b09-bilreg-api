using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public interface IPasienLogDal:
    IInsertBulk<PasienLogModel>,
    IDelete<IPasienKey>,
    IListData<PasienLogModel, IPasienKey>
{
}