using Bilreg.Application.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisFeature;

public class PetugasMedisRepo : IPetugasMedisRepo
{
    private readonly IPetugasMedisDal _petugasMedisDal;
    private readonly IPetugasMedisSatTugasDal _ptgMedisSatTugasDal;
    private readonly IPetugasMedisLayananDal _ptgMedisLayananDal;

    public PetugasMedisRepo(IPetugasMedisDal petugasMedisDal, 
        IPetugasMedisSatTugasDal ptgMedisSatTugasDal, 
        IPetugasMedisLayananDal ptgMedisLayananDal)
    {
        _petugasMedisDal = petugasMedisDal;
        _ptgMedisSatTugasDal = ptgMedisSatTugasDal;
        _ptgMedisLayananDal = ptgMedisLayananDal;
    }

    public void SaveChanges(PetugasMedisType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _petugasMedisDal.Update(PetugasMedisDto.FromModel(model)),
                onNone: () => _petugasMedisDal.Insert(PetugasMedisDto.FromModel(model))
            );
        _ptgMedisSatTugasDal.Delete(model);
        _ptgMedisLayananDal.Delete(model);
        _ptgMedisSatTugasDal.Insert(model.ListSatTugas.Select(x => PetugasMedisSatTugasDto.Create(model, x)));
        _ptgMedisLayananDal.Insert(model.ListLayanan.Select(x => PetugasMedisLayananDto.Create(model, x)));
    }

    public MayBe<PetugasMedisType> LoadEntity(IPetugasMedisKey key)
    {
        var hdr = _petugasMedisDal.GetData(key);
        var listSatTgs = _ptgMedisSatTugasDal.ListData(key)?.ToList() ?? [];
        var listSatTgsType = listSatTgs.Select(x => x.ToModel());
        var listLyn = _ptgMedisLayananDal.ListData(key)?.ToList() ?? [];
        var listLynType = listLyn.Select(x => x.ToModel());
        var model = hdr?.ToModel(listLynType, listSatTgsType);
        return MayBe.From(model!);
    }

    public void DeleteEntity(IPetugasMedisKey key)
    {
        _petugasMedisDal.Delete(key);
        _ptgMedisSatTugasDal.Delete(key);
        _ptgMedisLayananDal.Delete(key);
    }

    public IEnumerable<PetugasMedisView> ListData()
    {
        throw new NotImplementedException();
    }
}