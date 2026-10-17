using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WaitingListFeature;

public class WaitingListRepo : IWaitingListRepo
{
    private readonly IWaitingListDal _dal;

    public WaitingListRepo(IWaitingListDal dal) => _dal = dal;

    public void SaveChanges(WaitingListModel model)
    {
        var dto = WaitingListDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }

    public void Save(WaitingListModel model) => SaveChanges(model);

    public MayBe<WaitingListModel> LoadEntity(IWaitingListKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<WaitingListModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public MayBe<WaitingListModel> LoadActiveByRegId(string regId)
    {
        var dto = _dal.GetActiveByRegId(regId);
        if (dto is null)
            return MayBe<WaitingListModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public bool HasActiveByRegId(string regId) => LoadActiveByRegId(regId).HasValue;

    public IEnumerable<WaitingListModel> ListByRegId(string regId) =>
        _dal.ListByReg(regId).Select(x => x.ToModel());
}
