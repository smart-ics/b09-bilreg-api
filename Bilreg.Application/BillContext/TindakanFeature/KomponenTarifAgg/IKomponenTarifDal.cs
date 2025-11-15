using Bilreg.Domain.BillContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;

public interface IKomponenTarifDal:
    IInsert<KomponenType>,
    IUpdate<KomponenType>,
    IDelete<IKomponenKey>,
    IGetData<KomponenType, IKomponenKey>,
    IListData<KomponenType>
{
}