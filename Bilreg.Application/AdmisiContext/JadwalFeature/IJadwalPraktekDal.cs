using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JadwalFeature;

public interface IJadwalPraktekDal :
    IInsert<JadwalPraktekType>,
    IUpdate<JadwalPraktekType>,
    IDelete<IJadwalPraktekKey>,
    IGetDataMayBe<JadwalPraktekType, IJadwalPraktekKey>,
    IListDataMayBe<JadwalPraktekType, IPetugasMedisKey>,
    IListDataMayBe<JadwalPraktekType, ISmfKey>
{
}