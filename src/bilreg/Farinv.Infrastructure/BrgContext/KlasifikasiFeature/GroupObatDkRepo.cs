using Farinv.Application.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public class GroupObatDkRepo : IGroupObatDkRepo
{
    private readonly IGroupObatDkDal _groupObatDkDal;

    public GroupObatDkRepo(IGroupObatDkDal groupObatDkDal)
    {
        _groupObatDkDal = groupObatDkDal;
    }

    public void SaveChanges(GroupObatDkType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _groupObatDkDal.Update(GroupObatDkDto.FromModel(model)),
                onNone: () => _groupObatDkDal.Insert(GroupObatDkDto.FromModel(model)));
    }

    public MayBe<GroupObatDkType> LoadEntity(IGroupObatDkKey key)
    {
        var dto = _groupObatDkDal.GetData(key);
        if (dto is null)
            return MayBe<GroupObatDkType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IGroupObatDkKey key)
    {
        _groupObatDkDal.Delete(key);
    }

    public IEnumerable<GroupObatDkType> ListData()
    {
        var listDto = _groupObatDkDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}