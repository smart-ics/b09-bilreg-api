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

    public IEnumerable<PetugasMedisView> ListData(ISatTugasKey filter)
    {
        var pegSatTugasMeds = _ptgMedisSatTugasDal.ListData(filter) ?? [];
        var listPeg = _petugasMedisDal.ListData() ?? [];

        var result =
        from peg in listPeg
        join sat in pegSatTugasMeds
            on peg.fs_kd_peg equals sat.fs_kd_peg
        group sat by peg into g
        select new PetugasMedisView(
            PetugasMedisId: g.Key.fs_kd_peg,
            PetugasMedisName: g.Key.fs_nm_peg,
            NamaSingkat: g.Key.fs_nm_alias,
            Smf: new SmfType(g.Key.fs_kd_smf, g.Key.fs_nm_smf),
            ListSatTugas: g.Select(x => x.ToModel())
        );

        return result;
    }

    public IEnumerable<PpaLayananView> ListData(ISatTugasKey satTgsKey, IInstalasiDkKey instDkKey)
    {
        var listPtgMdsLyn = _ptgMedisLayananDal.ListData(satTgsKey, instDkKey)?.ToList()
            ?? throw new ArgumentException("Petugas medis layanan not found");

        return listPtgMdsLyn;
    }

    public IEnumerable<PpaType> ListData(ISatTugasKey satTugasKey, IEnumerable<IGroupSpesialisKey> listOfgroupSpesialKey)
    {
        var listDokter = _ptgMedisLayananDal.ListData(satTugasKey) ?? [];
        var listPeg = _petugasMedisDal.ListData() ?? [];

        var filterIds = new HashSet<string>(
            listOfgroupSpesialKey.Select(f => f.GroupSpesialisId), StringComparer.OrdinalIgnoreCase);

        var result = listDokter
            .Where(x => filterIds.Contains(x.GroupSpesialisId) & 
                        x.fb_utama == 1)
            .Select(x =>
            {
                var ptgMedis = listPeg.FirstOrDefault(y => y.fs_kd_peg == x.PpaId)
                    ?? new PpaDto("", "", "", "", "");
                var listLyn = listDokter
                    .Where(dokLyn => dokLyn.PpaId == x.PpaId);
                var listSatTgs = _ptgMedisSatTugasDal.ListData(PpaType.Key(x.PpaId)) ?? [];
                return new PpaType(
                    x.PpaId,
                    x.fs_nm_peg,
                    ptgMedis.fs_nm_alias,
                    new SmfType(ptgMedis.fs_kd_smf, ptgMedis.fs_nm_smf),
                    listLyn.Select(lyn => new PpaLayananType(
                        new LayananReff(lyn.LayananId, lyn.fs_nm_layanan), Convert.ToBoolean(lyn.fb_utama))),
                    new List<PpaSatTugasType>());
                    //listSatTgs.Select(stg => new PetugasMedisSatTugasType(
                    //    new SatTugasType(stg.fs_kd_sat_tugas, stg.fs_nm_sat_tugas, ptgMedis.)),
                    //    Convert.ToBoolean(stg.fn_utama));
            });

        return result;
    }
}