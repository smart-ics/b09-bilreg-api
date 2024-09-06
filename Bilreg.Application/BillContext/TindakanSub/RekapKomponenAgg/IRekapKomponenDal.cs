using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.RekapKomponenAgg
{
    public interface IRekapKomponenDal:
        IInsert<RekapKomponenModel>,
        IUpdate<RekapKomponenModel>,
        IDelete<IRekapKomponenKey>,
        IGetData<RekapKomponenModel,IRekapKomponenKey>,
        IListData<RekapKomponenModel>
    {
    }
}