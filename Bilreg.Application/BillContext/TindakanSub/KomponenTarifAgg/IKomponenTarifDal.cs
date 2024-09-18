using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;

public interface IKomponenTarifDal:
    IInsert<KomponenModel>,
    IUpdate<KomponenModel>,
    IDelete<IKomponenKey>,
    IGetData<KomponenModel, IKomponenKey>,
    IListData<KomponenModel>
{
}