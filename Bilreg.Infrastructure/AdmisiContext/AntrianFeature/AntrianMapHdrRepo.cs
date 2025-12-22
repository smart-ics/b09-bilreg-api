using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
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
        _mapDal.Insert(listDtlDto);
    }

    public MayBe<AntrianMapHdrModel> LoadEntity(IAntrianMapHdrKey key)
    {
        var hdr = _hdrDal.GetData(key);
        var listDtl = _mapDal.ListData(key)?.ToList() ?? [];
        var model = hdr?.ToModel(listDtl);
        return MayBe.From(model!);
    }
}
