using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JadwalFeature;

public interface IJadwalPraktekDal :
    IInsert<JadwalPraktekType>,
    IUpdate<JadwalPraktekType>,
    IDelete<IJadwalPraktekKey>,
    IGetDataMayBe<JadwalPraktekType, IJadwalPraktekKey>,
    IListData<JadwalPraktekType, IPetugasMedisKey>,
    IListData<JadwalPraktekType, ISmfKey>
{
}