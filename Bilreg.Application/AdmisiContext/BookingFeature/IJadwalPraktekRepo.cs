using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IJadwalPraktekRepo : 
    ISaveChange<JadwalPraktekType>,
    ILoadEntity<JadwalPraktekType, IJadwalPraktekKey>,
    IDeleteEntity<IJadwalPraktekKey>,
    IListData<JadwalPraktekType>,
    IListData<JadwalPraktekType, IPpaKey>,
    IListData<JadwalPraktekType, ILayananKey>,
    IListData<JadwalPraktekType, ILayananDkKey>,
    IListData<JadwalPraktekType, IGroupSpesialisKey>
{
}