using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;

public interface IKomponenTarifDal:
    IInsert<KomponenTarifModel>,
    IUpdate<KomponenTarifModel>,
    IDelete<IKomponenTarifKey>,
    IGetData<KomponenTarifModel, IKomponenTarifKey>,
    IListData<KomponenTarifModel>
{
}