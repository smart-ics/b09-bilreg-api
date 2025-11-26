using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public class PpaRepo : IPpaRepo
{
    private readonly IPpaDal _ppaDal;
    private readonly IPpaSatTugasDal _ppaSatTugasDal;
    private readonly IPpaLayananDal _ppaLayananDal;

    public PpaRepo(IPpaDal ppaDal, 
        IPpaSatTugasDal ppaSatTugasDal, 
        IPpaLayananDal ppaLayananDal)
    {
        _ppaDal = ppaDal;
        _ppaSatTugasDal = ppaSatTugasDal;
        _ppaLayananDal = ppaLayananDal;
    }

    public void SaveChanges(PpaType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _ppaDal.Update(PpaDto.FromModel(model)),
                onNone: () => _ppaDal.Insert(PpaDto.FromModel(model))
            );
        _ppaSatTugasDal.Delete(model);
        _ppaLayananDal.Delete(model);
        _ppaSatTugasDal.Insert(model.ListSatTugas.Select(x => PpaSatTugasDto.Create(model, x)));
        _ppaLayananDal.Insert(model.ListLayanan.Select(x => PpaLayananDto.Create(model, x)));
    }

    public MayBe<PpaType> LoadEntity(IPpaKey key)
    {
        var listSatTgs = _ppaSatTugasDal.ListData(key)?.ToList() ?? [];
        var listSatTgsType = listSatTgs.Select(x => x.ToModel());
        
        var listLyn = _ppaLayananDal.ListData(key)?.ToList() ?? [];
        var listLynType = listLyn.Select(x => x.ToModel());
        
        var hdr = _ppaDal.GetData(key);
        var model = hdr?.ToModel(listLynType, listSatTgsType);
        return MayBe.From(model!);
    }

    public void DeleteEntity(IPpaKey key)
    {
        _ppaDal.Delete(key);
        _ppaSatTugasDal.Delete(key);
        _ppaLayananDal.Delete(key);
    }

    public IEnumerable<PpaView> ListData(IProfesiKey filter)
    {
        var listPpa = _ppaDal.ListData(filter) ?? [];
        var result = listPpa.Select(x => x.ToView());
        return result;
    }

    public IEnumerable<PpaView> ListData()
    {
        var listPpa = _ppaDal.ListData() ?? [];
        var result = listPpa.Select(x => x.ToView());
        return result;    
    }
}