using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class KomponenRepo : IKomponenRepo
{
    private readonly IKomponenDal _komponenDal;
    private readonly IKomponenSatTugasDal _komponenSatTugasDal;

    public KomponenRepo(IKomponenDal komponenDal, IKomponenSatTugasDal komponenSatTugasDal)
    {
        _komponenDal = komponenDal;
        _komponenSatTugasDal = komponenSatTugasDal;
    }

    public void SaveChanges(KomponenType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _komponenDal.Update(KomponenDto.FromModel(model)),
                onNone: () => _komponenDal.Insert(KomponenDto.FromModel(model)));

        _komponenSatTugasDal.Delete(model);
        var listSatTugas = model.ListSatTugas
            .Select(x => KomponenSatTugasDto.FromModel(model.KomponenId, x))
            .ToList();
        if (listSatTugas.Count > 0)
            _komponenSatTugasDal.Insert(listSatTugas);
    }

    public MayBe<KomponenType> LoadEntity(IKomponenKey key)
    {   
        var dto = _komponenDal.GetData(key);
        if (dto is null)
            return MayBe<KomponenType>.None;

        var listSatTugasDto = _komponenSatTugasDal.ListData(key)?.ToList() ?? [];
        var listSatTugas = listSatTugasDto.Select(x => x.ToModel());

        var model = dto.ToModel(listSatTugas);
        return MayBe.From(model);
    }

    public void DeleteEntity(IKomponenKey key)
    {
        _komponenSatTugasDal.Delete(key);
        _komponenDal.Delete(key);
    }

    public IEnumerable<KomponenType> ListData(IEnumerable<IKomponenKey> filter)
    {
        var filterList = filter.ToList();
        if (!filterList.Any())
            return [];

        var listDto = _komponenDal.ListData()
            .Where(x => filterList.Any(f => f.KomponenId == x.fs_kd_detil_tarif))
            .ToList();

        var result = listDto.Select(dto =>
        {
            var listSatTugasDto = _komponenSatTugasDal.ListData(KomponenType.Key(dto.fs_kd_detil_tarif))?.ToList() ?? [];
            var listSatTugas = listSatTugasDto.Select(x => x.ToModel());
            return dto.ToModel(listSatTugas);
        }).ToList();
        
        return result;
    }
}
