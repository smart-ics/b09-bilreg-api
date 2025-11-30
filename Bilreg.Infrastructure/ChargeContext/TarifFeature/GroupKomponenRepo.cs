using Bilreg.Application.BillContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class GroupKomponenRepo : IGroupKomponenRepo
{
    private readonly IGroupKomponenDal _groupKomponenDal;
    public GroupKomponenRepo(IGroupKomponenDal groupKomponenDal)
    {
        _groupKomponenDal = groupKomponenDal;
    }
    public void SaveChanges(GroupKomponenType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _groupKomponenDal.Update(GroupKomponenDto.FromModel(model)),
                onNone: () => _groupKomponenDal.Insert(GroupKomponenDto.FromModel(model)));
    }

    public MayBe<GroupKomponenType> LoadEntity(IGroupKomponenKey key)
    {   
        var dto = _groupKomponenDal.GetData(key);
        if (dto is null)
            return MayBe<GroupKomponenType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IGroupKomponenKey key)
    {
        _groupKomponenDal.Delete(key);
    }

    public IEnumerable<GroupKomponenType> ListData()
    {
        var listDto = _groupKomponenDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
