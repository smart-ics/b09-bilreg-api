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
        var hdr = _ppaDal.GetData(key);
        var listSatTgs = _ppaSatTugasDal.ListData(key)?.ToList() ?? [];
        var listSatTgsType = listSatTgs.Select(x => x.ToModel());
        var listLyn = _ppaLayananDal.ListData(key)?.ToList() ?? [];
        var listLynType = listLyn.Select(x => x.ToModel());
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
        var pegSatTugasMeds = _ppaSatTugasDal.ListData(filter) ?? [];
        var listPeg = _ppaDal.ListData() ?? [];

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

    public IEnumerable<PpaView> ListData()
    {
        throw new NotImplementedException();
    }
}