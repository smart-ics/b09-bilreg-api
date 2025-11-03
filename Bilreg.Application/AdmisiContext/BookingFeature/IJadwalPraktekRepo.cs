using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IJadwalPraktekRepo : 
    ISaveChange<JadwalPraktekType>,
    ILoadEntity<JadwalPraktekType, IJadwalPraktekKey>,
    IDeleteEntity<IJadwalPraktekKey>,
    IListData<JadwalPraktekType, IPetugasMedisKey>
{
}