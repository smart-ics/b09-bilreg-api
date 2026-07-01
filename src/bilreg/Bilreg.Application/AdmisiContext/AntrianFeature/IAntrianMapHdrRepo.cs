using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAntrianMapRepo :
    ISaveChange<AntrianMapModel>,
    ILoadEntity<AntrianMapModel, IAntrianMapKey>
{
    IEnumerable<AntrianMapHdrView> ListData(ILayananKey lynKey, IPpaKey ppaKey, DateOnly tglPraktek);
    MayBe<AntrianMapModel> Find(JadwalPraktekType jadwal, DateOnly tgl);
    IEnumerable<AntrianMapDetilModel> ListDetil(JadwalPraktekType jadwal, DateOnly tgl);
}


public record AntrianMapHdrView(string AntrianMapId, string JadwalId, PpaReff Dokter, 
    LayananReff Layanan, DateOnly TglJadwal, TimeOnly JamJadwal, 
    TimeOnly JamPraktek) : IAntrianMapKey;