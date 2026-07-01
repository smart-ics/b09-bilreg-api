using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class GroupTarifRepo : IGroupTarifRepo
{
    private readonly IGroupTarifDal _groupTarifDal;
    public GroupTarifRepo(IGroupTarifDal groupTarifDal)
    {
        _groupTarifDal = groupTarifDal;
    }
    public void SaveChanges(GroupTarifType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _groupTarifDal.Update(GroupTarifDto.FromModel(model)),
                onNone: () => _groupTarifDal.Insert(GroupTarifDto.FromModel(model)));
    }

    public MayBe<GroupTarifType> LoadEntity(IGroupTarifKey key)
    {   
        var dto = _groupTarifDal.GetData(key);
        if (dto is null)
            return MayBe<GroupTarifType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IGroupTarifKey key)
    {
        _groupTarifDal.Delete(key);
    }

    public IEnumerable<GroupTarifType> ListData()
    {
        var listDto = _groupTarifDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
