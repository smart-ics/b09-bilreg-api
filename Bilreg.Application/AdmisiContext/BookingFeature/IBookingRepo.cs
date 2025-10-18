using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananSub;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IBookingRepo :
    ISaveChange<BookingModel>,
    ILoadEntity<BookingModel, IBookingKey>,
    IDeleteEntity<IBookingKey>
{
}
