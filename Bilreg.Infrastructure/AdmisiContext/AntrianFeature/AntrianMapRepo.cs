using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class AntrianMapRepo : IAntrianMapRepo
{
    private readonly IAntrianMapDal _antrianMapDal;
    private readonly IAntrianMapDetilDal _antrianMapDetilDal;

    public AntrianMapRepo(IAntrianMapDal hdrDal,
        IAntrianMapDetilDal mapDal)
    {
        _antrianMapDal = hdrDal;
        _antrianMapDetilDal = mapDal;
    }
    public void SaveChanges(AntrianMapModel model)
    {
        var antrianMap = _antrianMapDal.GetData(model);
        if (antrianMap is null)
            _antrianMapDal.Insert(AntrianMapDto.FromModel(model));
        else
            _antrianMapDal.Update(AntrianMapDto.FromModel(model));
        
        var listDtlDto = model.ListMap.Select(x => AntrianMapDetilDto.FromModel(x, model));

        _antrianMapDetilDal.Delete(model);
        listDtlDto.ForEach(x => _antrianMapDetilDal.Insert(x));
    }

    public MayBe<AntrianMapModel> LoadEntity(IAntrianMapKey key)
    {
        var hdr = _antrianMapDal.GetData(key);
        if (hdr is null)
            return MayBe<AntrianMapModel>.None;

        var listDtlDto = _antrianMapDetilDal.ListData(key)?.ToList() ?? [];
        var model = hdr.ToModel(listDtlDto.Select(x => x.ToModel()));
        return MayBe.From(model);
    }   

    public IEnumerable<AntrianMapHdrView> ListData(ILayananKey lynKey, IPpaKey ppaKey, DateOnly tglBerobat)
    {
        var listAnt = _antrianMapDal.ListData(lynKey, ppaKey, tglBerobat)?.ToList() ?? [];
        var result = listAnt.Select(x => x.ToView())?.ToList() ?? [];
        
        return result;
    }
}

