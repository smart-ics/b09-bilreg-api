using Bilreg.Domain.AdmisiContext.BookingFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IBookingRepo :
    ISaveChange<BookingModel>,
    ILoadEntity<BookingModel, IBookingKey>,
    IDeleteEntity<IBookingKey>,
    IListData<BookingModel, Periode>
{
    IEnumerable<BookingModel> ListDataTglBerobat(Periode periode);
}
