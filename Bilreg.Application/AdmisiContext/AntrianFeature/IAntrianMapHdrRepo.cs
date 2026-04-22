using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAntrianMapHdrRepo :
    ISaveChange<AntrianMapModel>,
    ILoadEntity<AntrianMapModel, IAntrianMapKey>
{
    IEnumerable<AntrianMapHdrView> ListData(ILayananKey lynKey, IPpaKey ppaKey, DateOnly tglPraktek);
    void Migrasi(DateTime date);

}


public record AntrianMapHdrView(string JadwalId, PpaReff dokter, 
    LayananReff Layanan, DateOnly TglJadwal, TimeOnly JamJadwal, 
    TimeOnly JamPraktek);