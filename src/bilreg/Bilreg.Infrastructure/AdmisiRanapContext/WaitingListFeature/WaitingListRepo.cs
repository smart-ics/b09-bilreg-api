using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature;

public class WaitingListRepo : IWaitingListRepo
{
    private readonly IWaitingListDal _dal;

    public WaitingListRepo(IWaitingListDal dal) => _dal = dal;

    public void SaveChanges(WaitingListModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(WaitingListDto.FromModel(model)),
                onNone: () => _dal.Insert(WaitingListDto.FromModel(model)));
    }

    public MayBe<WaitingListModel> LoadEntity(IWaitingListKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<WaitingListModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public bool HasActiveByRegId(string regId) => _dal.HasActiveByRegId(regId);
}
