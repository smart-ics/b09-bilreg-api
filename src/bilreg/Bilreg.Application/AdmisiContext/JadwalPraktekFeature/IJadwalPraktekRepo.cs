using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

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
    void Migrasi();

}