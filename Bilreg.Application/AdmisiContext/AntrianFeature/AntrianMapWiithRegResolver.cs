using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAntrianMapWithRegResolver : INunaResolver<(AntrianMapModel, AntrianMapDetilModel), JadwalPraktekType, DateOnly, RegModel>
{
} 

public class AntrianMapWithRegResolver :  IAntrianMapWithRegResolver
{
    private readonly IAntrianMapRepo _antrianMapRepo;
    private readonly IGetGrupJaminanJetliService _getGrupJaminanJetliService;
    public AntrianMapWithRegResolver(IAntrianMapRepo antrianMapRepo, 
        IGetGrupJaminanJetliService getGrupJaminanJetliService)
    {
        _antrianMapRepo = antrianMapRepo;
        _getGrupJaminanJetliService = getGrupJaminanJetliService;
    }

    public Result<(AntrianMapModel, AntrianMapDetilModel)> Resolve(JadwalPraktekType jadwal, DateOnly tgl, RegModel reg)
    {
        // /*
        //     1. Resolve Jadwal + Tgl jadi AntrianMapId (Key)
        //     2. Jika not resolve => Create New Model (Header)
        //     3. Load Model()
        //  */
        var antrianMap = _antrianMapRepo.Find(jadwal, tgl)
            .Match(
                onSome: x => x,
                onNone: () => AntrianMapModel.CreateFromJadwal(jadwal, tgl)
            );

        if (!antrianMap.ListMap.Any())
        {
            var listDetilDb = _antrianMapRepo.ListDetil(jadwal, tgl)?.ToList() ?? [];
            antrianMap.AttachDetil(listDetilDb);
        }
        
        if (!antrianMap.ListMap.Any())
            antrianMap.SeedingMap();

        var req = new GetGrupJaminanJetliRequest(reg.TipeJaminan);
        var grupJmnResp = _getGrupJaminanJetliService.Execute(req);
        var flag = grupJmnResp.GroupJaminanId == "JKN" ? "BPJS" : "UMUM";
        var emptyMap = antrianMap.ListMap
            .Where(x => x.Flag == flag)
            .Where(x => x.ReffId.Trim() == "")
            .OrderBy(x => x.NoUrut)
            .FirstOrDefault();
        emptyMap = emptyMap ??
                   antrianMap.ListMap
                       .Where(x => x.Flag == "AUTO")
                       .Where(x => x.ReffId.Trim() == "")
                       .OrderBy(x => x.NoUrut)
                       .FirstOrDefault();
        
        AntrianMapDetilModel newDetil;
        if (emptyMap is null)
        {
            newDetil = antrianMap.AddAuto(reg);
        }
        else
        {
            emptyMap.SetPasien(reg.Pasien.PasienName, reg.Pasien.PasienId, reg.RegId);
            newDetil = emptyMap;
        }
        return Result<(AntrianMapModel, AntrianMapDetilModel)>.Success((antrianMap, newDetil));
    }

}

public interface INunaResolver<TOut, in TIn1, in TIn2>
{
     Result<TOut> Resolve(TIn1 in1, TIn2 in2);
}

public interface INunaResolver<TOut, in TIn1, in TIn2, in TIn3>
{
    Result<TOut> Resolve(TIn1 in1, TIn2 in2, TIn3 in3);
}

public interface INunaResolver<TOut, in TIn1, in TIn2, in TIn3, in TIn4>
{
    Result<TOut> Resolve(TIn1 in1, TIn2 in2, TIn3 in3, TIn4 in4);
}