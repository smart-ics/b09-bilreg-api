using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;

public interface IPolisDal :
    IInsert<PolisModel>,
    IUpdate<PolisModel>,
    IDelete<IPolisKey>,
    IGetData<PolisModel, IPolisKey>,
    IListData<PolisModel, IPasienKey>
{
}