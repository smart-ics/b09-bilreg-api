using Farinv.Application.BrgContext.BrgFeature;
using Farinv.Domain.BrgContext.BrgFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public class GroupRekDkRepo : IGroupRekDkRepo
{
    private readonly IGroupRekDkDal _groupRekDkDal;
    public GroupRekDkRepo(IGroupRekDkDal groupRekDkDal)
    {
        _groupRekDkDal = groupRekDkDal;
    }
    
    public void SaveChanges(GroupRekDkType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _groupRekDkDal.Update(GroupRekDkDto.FromModel(model)),
                onNone: () => _groupRekDkDal.Insert(GroupRekDkDto.FromModel(model)));
    }
    
    public MayBe<GroupRekDkType> LoadEntity(IGroupRekDkKey key)
    {   
        var dto = _groupRekDkDal.GetData(key);
        if (dto is null)
            return MayBe<GroupRekDkType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }
    
    public void DeleteEntity(IGroupRekDkKey key)
    {
        _groupRekDkDal.Delete(key);
    }
    
    public IEnumerable<GroupRekDkType> ListData()
    {
        var listDto = _groupRekDkDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}