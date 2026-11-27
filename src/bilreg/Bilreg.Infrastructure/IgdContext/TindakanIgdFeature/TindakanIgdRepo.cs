using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;

public class TindakanIgdRepo : ITindakanIgdRepo
{
    private readonly ITindakanIgdDal _dal;
    private readonly ITindakanIgdPpaDal _ppaDal;
    private readonly ITindakanIgdBarangDal _barangDal;

    public TindakanIgdRepo(
        ITindakanIgdDal dal,
        ITindakanIgdPpaDal ppaDal,
        ITindakanIgdBarangDal barangDal)
    {
        _dal = dal;
        _ppaDal = ppaDal;
        _barangDal = barangDal;
    }

    public void SaveChanges(TindakanIgdModel model)
    {
        var dto = TindakanIgdDto.FromModel(model);
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);

        var listPpaDto = model.ListPpa
            .Select(x => TindakanIgdPpaDto.FromModel(x, model.TindakanIgdId))
            .ToList();
        _ppaDal.Delete(model);
        if (listPpaDto.Count > 0)
            _ppaDal.Insert(listPpaDto);

        var listBarangDto = model.ListBarang
            .Select(x => TindakanIgdBarangDto.FromModel(x, model.TindakanIgdId))
            .ToList();
        _barangDal.Delete(model);
        if (listBarangDto.Count > 0)
            _barangDal.Insert(listBarangDto);
    }

    public void DeleteEntity(ITindakanIgdKey key)
    {
        _ppaDal.Delete(key);
        _barangDal.Delete(key);
        _dal.Delete(key);
    }

    public void MarkBarangVoid(ITindakanIgdKey key)
    {
        _barangDal.MarkVoid(key);
    }

    public MayBe<TindakanIgdModel> LoadEntity(ITindakanIgdKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<TindakanIgdModel>.None;

        var listPpa = (_ppaDal.ListData(key)?.ToList() ?? [])
            .Select(x => x.ToModel());
        var listBarang = (_barangDal.ListData(key)?.ToList() ?? [])
            .Select(x => x.ToModel());

        return MayBe.From(dto.ToModel(listPpa, listBarang));
    }

    public IEnumerable<TindakanIgdView> ListData(IIgdVisitKey filter)
        => (_dal.ListData(filter)?.ToList() ?? []).Select(x => x.ToView());

    public bool AnyForVisit(IIgdVisitKey visit) => _dal.CountForVisit(visit) > 0;
}
