using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class KarcisRepo : IKarcisRepo
{
    private readonly IKarcisDal _karcisDal;
    private readonly IKarcisKomponenDal _karcisKomponenDal;
    private readonly IKarcisLayananDal _karcisLayananDal;

    public KarcisRepo(IKarcisDal karcisDal, 
        IKarcisKomponenDal karcisKomponenDal, 
        IKarcisLayananDal karcisLayananDal)
    {
        _karcisDal = karcisDal;
        _karcisKomponenDal = karcisKomponenDal;
        _karcisLayananDal = karcisLayananDal;
    }

    public void SaveChanges(KarcisType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _karcisDal.Update(KarcisDto.FromModel(model)),
                onNone: () => _karcisDal.Insert(KarcisDto.FromModel(model))
            );

        var listKomponen = _karcisKomponenDal.ListData(model)?.ToList() ?? [];
        _karcisKomponenDal.Delete(model);
        _karcisKomponenDal.Insert(listKomponen);
        
        var listLayanan = _karcisLayananDal.ListData(model)?.ToList() ?? [];
        _karcisLayananDal.Delete(model);
        _karcisLayananDal.Insert(listLayanan);
    }

    public void Delete(IKarcisKey key)
    {
        _karcisKomponenDal.Delete(key);
        _karcisLayananDal.Delete(key);
        _karcisDal.Delete(key);
    }

    public MayBe<KarcisType> LoadEntity(IKarcisKey key)
    {
        var hdr = _karcisDal.GetData(key);
        var listKomp = _karcisKomponenDal.ListData(key)?.ToList() ?? [];
        var listKompType = listKomp.Select(x => x.ToModel());

        var listLyn = _karcisLayananDal.ListData(key)?.ToList() ?? [];
        var listLynType = listLyn.Select(x => x.ToModel());
        var model = hdr?.ToModel(listKompType, listLynType);
        return MayBe.From(model!);
    }

    public IEnumerable<KarcisView> ListData(IInstalasiDkKey filter)
    {
        var listDto = _karcisDal.ListData(filter)?.ToList() ?? [];
        var result = listDto.Select(x => new KarcisView(
            x.fs_kd_karcis,
            x.fs_nm_karcis,
            new InstalasiDkType(x.fs_kd_instalasi_dk, x.fs_nm_instalasi_dk),
            new TarifReff(x.fs_kd_tarif, x.fs_nm_tarif),
            x.fn_karcis));
        return result;
    }
}