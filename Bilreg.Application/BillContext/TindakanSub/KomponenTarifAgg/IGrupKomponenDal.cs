using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg
{
    public interface IGrupKomponenDal:
        IInsert<GrupKomponenModel>,
        IUpdate<GrupKomponenModel>,
        IDelete<IGrupKomponenKey>,
        IGetData<GrupKomponenModel,IGrupKomponenKey>,
        IListData<GrupKomponenModel>
    {
    }
}