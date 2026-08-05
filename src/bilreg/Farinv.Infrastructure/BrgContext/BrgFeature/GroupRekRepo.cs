using Farinv.Application.BrgContext.BrgFeature;
using Farinv.Domain.BrgContext.BrgFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public class GroupRekRepo : IGroupRekRepo
{
    private readonly IGroupRekDal _groupRekDal;
    private readonly IGroupRekDkRepo _groupRekDkRepo;
    public GroupRekRepo(IGroupRekDal groupRekDal, IGroupRekDkRepo groupRekDkRepo)
    {
        _groupRekDal = groupRekDal;
        _groupRekDkRepo = groupRekDkRepo;
    }
    
    public void SaveChanges(GroupRekType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _groupRekDal.Update(GroupRekDto.FromModel(model)),
                onNone: () => _groupRekDal.Insert(GroupRekDto.FromModel(model)));
    }
    
    public MayBe<GroupRekType> LoadEntity(IGroupRekKey key)
    {   
        var dto = _groupRekDal.GetData(key);
        if (dto is null)
            return MayBe<GroupRekType>.None;

        var model = dto.ToModel();
        return MayBe.From(model);
    }
    
    public void DeleteEntity(IGroupRekKey key)
    {
        _groupRekDal.Delete(key);
    }
    
    public IEnumerable<GroupRekType> ListData()
    {
        var listDto = _groupRekDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        
        return result;
    }
}