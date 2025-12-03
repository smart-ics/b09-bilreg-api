using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public class GroupSpesialisRepo : IGroupSpesialisRepo
{
    private readonly IGroupSpesialisDal _dal;

    public GroupSpesialisRepo(IGroupSpesialisDal dal)
    {
        _dal = dal;
    }

    public MayBe<GroupSpesialisType> LoadEntity(IGroupSpesialisKey key)
    {
        var groupSpesialis = _dal.GetData(key);
        return MayBe.From(groupSpesialis!);
    }

    public IEnumerable<GroupSpesialisType> ListData()
    {
        var listData = _dal.ListData()?.ToList() ?? [];
        return listData;
    }
}
