using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public class AntrianMapResolver : INunaResolver<AntrianMapModel, JadwalPraktekType, DateOnly>
{
    private readonly IAntrianMapRepo _antrianMapRepo;
    public AntrianMapResolver(IAntrianMapRepo antrianMapRepo)
    {
        _antrianMapRepo = antrianMapRepo;
    }

    public Result<AntrianMapModel> Resolve(JadwalPraktekType jadwal, DateOnly tgl)
    {
        // /*
        //     1. Resolve Jadwal + Tgl jadi AntrianMapId (Key)
        //     2. Jika not resolve => Create New Model (Header)
        //     3. Load Model()
        //  */
        // var antrianMap
        // if (!antrianMap.HasValue)
        throw new NotImplementedException();
    }

    public MayBe<AntrianMapModel> GetAntrianMap(JadwalPraktekType jadwal, DateOnly tgl)
    {
        throw new NotImplementedException();
    }
}

public interface INunaResolver<TOut, in TIn1, in TIn2>
{
     Result<TOut> Resolve(TIn1 in1, TIn2 in2);
}