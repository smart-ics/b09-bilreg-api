using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public class PpaRepo : IPpaRepo
{
    private readonly IPpaDal _petugasMedisDal;
    private readonly IPetugasMedisSatTugasDal _ptgMedisSatTugasDal;
    private readonly IPpaLayananDal _ptgMedisLayananDal;

    public PpaRepo(IPpaDal petugasMedisDal, 
        IPetugasMedisSatTugasDal ptgMedisSatTugasDal, 
        IPpaLayananDal ptgMedisLayananDal)
    {
        _petugasMedisDal = petugasMedisDal;
        _ptgMedisSatTugasDal = ptgMedisSatTugasDal;
        _ptgMedisLayananDal = ptgMedisLayananDal;
    }

    public void SaveChanges(PpaType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _petugasMedisDal.Update(PpaDto.FromModel(model)),
                onNone: () => _petugasMedisDal.Insert(PpaDto.FromModel(model))
            );
        _ptgMedisSatTugasDal.Delete(model);
        _ptgMedisLayananDal.Delete(model);
        _ptgMedisSatTugasDal.Insert(model.ListSatTugas.Select(x => PpaSatTugasDto.Create(model, x)));
        _ptgMedisLayananDal.Insert(model.ListLayanan.Select(x => PpaLayananDto.Create(model, x)));
    }

    public MayBe<PpaType> LoadEntity(IPpaKey key)
    {
        var hdr = _petugasMedisDal.GetData(key);
        var listSatTgs = _ptgMedisSatTugasDal.ListData(key)?.ToList() ?? [];
        var listSatTgsType = listSatTgs.Select(x => x.ToModel());
        var listLyn = _ptgMedisLayananDal.ListData(key)?.ToList() ?? [];
        var listLynType = listLyn.Select(x => x.ToModel());
        var model = hdr?.ToModel(listLynType, listSatTgsType);
        return MayBe.From(model!);
    }

    public void DeleteEntity(IPpaKey key)
    {
        _petugasMedisDal.Delete(key);
        _ptgMedisSatTugasDal.Delete(key);
        _ptgMedisLayananDal.Delete(key);
    }

    public IEnumerable<PpaView> ListData(IProfesiKey filter)
    {
        var pegSatTugasMeds = _ptgMedisSatTugasDal.ListData(filter) ?? [];
        var listPeg = _petugasMedisDal.ListData() ?? [];

        var result =
        from peg in listPeg
        join sat in pegSatTugasMeds
            on peg.fs_kd_peg equals sat.fs_kd_peg
        group sat by peg into g
        select new PpaView(
            PetugasMedisId: g.Key.fs_kd_peg,
            PetugasMedisName: g.Key.fs_nm_peg,
            NamaSingkat: g.Key.fs_nm_alias,
            Smf: new SmfType(g.Key.fs_kd_smf, g.Key.fs_nm_smf),
            ListSatTugas: g.Select(x => x.ToModel())
        );

        return result;
    }
}