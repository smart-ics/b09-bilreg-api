using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

public interface IJadwalPraktekHarianRepo :
    ISaveChange<JadwalPraktekHarianType>,
    ILoadEntity<JadwalPraktekHarianType, IJadwalPraktekHarianKey>
{
    IEnumerable<JadwalPraktekHarianType> ListByDate(DateOnly tglPraktek);
    IEnumerable<JadwalPraktekHarianType> ListByDateAndDokter(DateOnly tglPraktek, IPpaKey dokter);
}
