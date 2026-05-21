using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.LabContext.LabComponentMasterFeature;

public class LabComponentMasterRepo : ILabComponentMasterRepo
{
    private readonly ILabComponentMasterDal _dal;

    public LabComponentMasterRepo(ILabComponentMasterDal dal)
    {
        _dal = dal;
    }

    public MayBe<LabComponentMasterModel> LoadEntity(ILabComponentMasterKey key)
    {
        var dto = _dal.GetData(key);
        return MayBe.From(dto?.ToModel()!);
    }

    public MayBe<LabComponentMasterModel> LoadByComponentCode(string componentCode)
    {
        var dto = _dal.GetByComponentCode(componentCode);
        return MayBe.From(dto?.ToModel()!);
    }

    public IEnumerable<LabComponentMasterModel> ListData()
    {
        return _dal.ListData().Select(x => x.ToModel());
    }

    public IEnumerable<LabComponentMasterModel> ListData(LabComponentMasterListFilter filter)
    {
        return _dal.ListData(filter).Select(x => x.ToModel());
    }
}
