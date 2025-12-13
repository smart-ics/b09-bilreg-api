using Bilreg.Domain.AdmisiContext.BookingFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IBookingRepo :
    ISaveChange<BookingModel>,
    ILoadEntity<BookingModel, IBookingKey>,
    ILoadEntity<BookingModel, string>,
    IDeleteEntity<IBookingKey>,
    IListData<BookingView, Periode>
{
    IEnumerable<BookingView> ListDataTglBerobat(Periode periode);
    IEnumerable<BookingExtView> ListDataExtApp(Periode periode);
}
