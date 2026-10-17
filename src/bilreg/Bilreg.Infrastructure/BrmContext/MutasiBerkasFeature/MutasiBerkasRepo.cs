using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasRepo : IMutasiBerkasRepo
{
    private readonly IMutasiBerkasDal _mutasiDal;
    private readonly IVolumeBerkasDal _volumeDal;

    public MutasiBerkasRepo(IMutasiBerkasDal mutasiDal, IVolumeBerkasDal volumeDal)
    {
        _mutasiDal = mutasiDal;
        _volumeDal = volumeDal;
    }

    public void SaveChanges(MutasiBerkasModel model)
    {
        using var trans = TransHelper.NewScope();

        var mutasiDto = MutasiBerkasDto.FromModel(model);
        var existing = _mutasiDal.GetByKey(model.MutasiBerkasId);
        if (existing is null)
        {
            _mutasiDal.Insert(mutasiDto);
        }
        else
        {
            _mutasiDal.Update(mutasiDto);
        }

        var volumeDto = VolumeBerkasDto.FromModel(
            regId: model.TrsBuatId,
            mrId: model.PasienId,
            volume: model.Volume);
        _volumeDal.Upsert(volumeDto);

        trans.Complete();
    }

    public MayBe<MutasiBerkasModel> LoadEntity(IMutasiBerkasKey key)
    {
        if (key is null || string.IsNullOrWhiteSpace(key.MutasiBerkasId))
            return MayBe<MutasiBerkasModel>.None;

        var dto = _mutasiDal.GetByKey(key.MutasiBerkasId);
        if (dto is null)
            return MayBe<MutasiBerkasModel>.None;

        var volumeDto = _volumeDal.GetData(dto.FS_KD_TRS_BUAT);
        var volume = volumeDto?.ToModel() ?? VolumeBerkasType.Default;

        return MayBe.From(dto.ToModel(volume));
    }

    public MayBe<MutasiBerkasModel> GetLatestActiveByMr(string mrId)
    {
        if (string.IsNullOrWhiteSpace(mrId))
            return MayBe<MutasiBerkasModel>.None;

        var dto = _mutasiDal.GetLatestActiveByMr(mrId);
        if (dto is null)
            return MayBe<MutasiBerkasModel>.None;

        var volumeDto = _volumeDal.GetData(dto.FS_KD_TRS_BUAT);
        var volume = volumeDto?.ToModel() ?? VolumeBerkasType.Default;

        return MayBe.From(dto.ToModel(volume));
    }

    public IEnumerable<MutasiBerkasModel> ListByTrsBuat(string trsBuatId)
    {
        if (string.IsNullOrWhiteSpace(trsBuatId))
            return [];

        var list = _mutasiDal.ListByTrsBuat(trsBuatId)?.ToList() ?? [];
        if (list.Count == 0)
            return [];

        var volumeDto = _volumeDal.GetData(trsBuatId);
        var volume = volumeDto?.ToModel() ?? VolumeBerkasType.Default;

        return list.Select(x => x.ToModel(volume));
    }
}
