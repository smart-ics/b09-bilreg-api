using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.DemografiFeature;

public class KelurahanRepo : IKelurahanRepo
{
    private readonly IKelurahanDal _kelurahanDal;

    public KelurahanRepo(IKelurahanDal kelurahanDal)
    {
        _kelurahanDal = kelurahanDal;
    }

    public void SaveChanges(KelurahanType model)
    {
        LoadEntity(model)
            .Match(
                onSome: x => _kelurahanDal.Update(KelurahanDto.FromModel(model)),
                onNone: () => _kelurahanDal.Insert(KelurahanDto.FromModel(model)));
    }

    public MayBe<KelurahanType> LoadEntity(IKelurahanKey key)
    {
        var dto = _kelurahanDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
        
    }

    public void DeleteEntity(IKelurahanKey key)
    {
        _kelurahanDal.Delete(key);
    }


    public IEnumerable<KelurahanView> ListData()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<KelurahanView> ListData(string filter)
    {
        var listDto = _kelurahanDal.ListData(filter);
        var response = listDto?
            .Select(x => new KelurahanView(
                x.fs_kd_kelurahan, x.fs_nm_kelurahan,
                x.fs_nm_kecamatan, x.fs_nm_kabupaten, x.fs_nm_propinsi))
            .ToList() ?? [];
        return response;
    }
}