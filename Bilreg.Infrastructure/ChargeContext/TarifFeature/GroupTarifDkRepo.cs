using Bilreg.Application.BillContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class GroupTarifDkRepo : IGroupTarifDkRepo
{
    private readonly IGroupTarifDkDal _groupTarifDkDal;
    public GroupTarifDkRepo(IGroupTarifDkDal groupTarifDkDal)
    {
        _groupTarifDkDal = groupTarifDkDal;
    }
    public void SaveChanges(GroupTarifDkType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _groupTarifDkDal.Update(GroupTarifDkDto.FromModel(model)),
                onNone: () => _groupTarifDkDal.Insert(GroupTarifDkDto.FromModel(model)));
    }

    public MayBe<GroupTarifDkType> LoadEntity(IGroupTarifDkKey key)
    {   
        var dto = _groupTarifDkDal.GetData(key);
        if (dto is null)
            return MayBe<GroupTarifDkType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IGroupTarifDkKey key)
    {
        _groupTarifDkDal.Delete(key);
    }

    public IEnumerable<GroupTarifDkType> ListData()
    {
        var listDto = _groupTarifDkDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
