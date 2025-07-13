using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg
{
    public interface IGrupKomponenDal:
        IInsert<GrupKomponenType>,
        IUpdate<GrupKomponenType>,
        IDelete<IGrupKomponenKey>,
        IGetData<GrupKomponenType,IGrupKomponenKey>,
        IListData<GrupKomponenType>
    {
    }
}