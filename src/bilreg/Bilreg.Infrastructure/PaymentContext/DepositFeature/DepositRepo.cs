using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PaymentContext.DepositFeature;

public class DepositRepo : IDepositRepo
{
    private readonly IDepositDal _depositDal;

    public DepositRepo(IDepositDal depositDal)
    {
        _depositDal = depositDal;
    }

    public void SaveChanges(DepositModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _depositDal.Update(DepositDto.FromModel(model)),
                onNone: () => _depositDal.Insert(DepositDto.FromModel(model)));
    }

    public MayBe<DepositModel> LoadEntity(IDepositId key)
    {
        var dto = _depositDal.GetData(key);
        if (dto is null)
            return MayBe<DepositModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<DepositModel> ListData(IRegKey key)
    {
        var listDto = _depositDal.ListData(key)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel());
    }
}