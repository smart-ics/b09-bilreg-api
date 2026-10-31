using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IRegDal :
    IInsert<RegDto>,
    IUpdate<RegDto>,
    IDelete<IRegKey>,
    IGetData<RegDto, IRegKey>,
    IGetData<RegDto, IBookingKey>,
    IListData<RegDto, Periode, ILayananKey>,
    IListData<RegDto, IPasienKey>,
    IListData<RegDto, DateTime>
{
    IEnumerable<RegDto> ListDataByName(Dictionary<string, string[]> listName);
}
