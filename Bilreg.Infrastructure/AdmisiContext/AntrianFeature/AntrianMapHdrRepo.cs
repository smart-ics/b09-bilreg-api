using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class AntrianMapHdrRepo : IAntrianMapHdrRepo
{
    private readonly IAntrianMapHdrDal _hdrDal;
    private readonly IAntrianMapDal _mapDal;
    public AntrianMapHdrRepo(IAntrianMapHdrDal hdrDal, 
        IAntrianMapDal mapDal)
    {
        _hdrDal = hdrDal;
        _mapDal = mapDal;
    }
    public void SaveChanges(AntrianMapHdrModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _hdrDal.Update(AntrianMapHdrDto.FromModel(model)),
                onNone: () => _hdrDal.Insert(AntrianMapHdrDto.FromModel(model))
            );
        var listDtlDto = model.ListMap.Select(x => AntrianMapDto.FromModel(x));

        _mapDal.Delete(model);
        listDtlDto.ForEach(x => _mapDal.Insert(x));
    }

    public MayBe<AntrianMapHdrModel> LoadEntity(IAntrianMapHdrKey key)
    {
        var hdr = _hdrDal.GetData(key);
        var listDtl = _mapDal.ListData(key)?.ToList() ?? [];
        var model = hdr?.ToModel(listDtl);
        return MayBe.From(model!);
    }

    public IEnumerable<AntrianMapHdrView> ListData(ILayananKey lynKey, IPpaKey ppaKey, DateOnly tglBerobat)
    {
        var listAnt = _hdrDal.ListData(lynKey, ppaKey, tglBerobat)?.ToList() ?? [];
        var result = listAnt.Select(x => x.ToView())?.ToList() ?? [];
        
        return result;
    }
}
