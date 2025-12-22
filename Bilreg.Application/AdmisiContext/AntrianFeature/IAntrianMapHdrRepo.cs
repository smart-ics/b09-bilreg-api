using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAntrianMapHdrRepo :
    ISaveChange<AntrianMapHdrModel>,
    ILoadEntity<AntrianMapHdrModel, IAntrianMapHdrKey>
{
    IEnumerable<AntrianMapHdrView> ListData(ILayananKey lynKey, IPpaKey ppaKey, DateOnly tglPraktek);
}


public record AntrianMapHdrView(string JadwalId, PpaReff dokter, 
    LayananReff Layanan, DateOnly TglJadwal, TimeOnly JamJadwal, 
    TimeOnly JamPraktek);