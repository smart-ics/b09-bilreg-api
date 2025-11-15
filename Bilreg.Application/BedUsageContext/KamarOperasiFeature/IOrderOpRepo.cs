using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IOrderOpRepo :
    ISaveChange<OrderOpModel>,
    ILoadEntity<OrderOpModel, IOrderOpKey>,
    IDeleteEntity<IOrderOpKey>,
    IListData<OrderOpView, Periode>
{
}

public record OrderOpView(string OrderOpId, string RegId, string PasienId, string PasienName,
    string NamaOperasi, JenisOperasiType JenisOperasi, DateTime PreferedDate);