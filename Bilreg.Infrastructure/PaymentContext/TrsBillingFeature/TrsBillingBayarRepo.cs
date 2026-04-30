using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public class TrsBillingBayarRepo : ITrsBillingBayarRepo
{
    private readonly ITrsBilling2GenDal _trsBilling2GenDal;
    public TrsBillingBayarRepo(ITrsBilling2GenDal trsBilling2GenDal)
    {
        _trsBilling2GenDal = trsBilling2GenDal;
    }

    public void SaveChanges(IEnumerable<TrsBilling2Model> model)
    {
        _trsBilling2GenDal.Delete(model.First());
        var listBillBayar = model
            .Select(x => TrsBilling2GenDto.FromModel(x));

        _trsBilling2GenDal.Insert(listBillBayar);
    }

    public void DeleteEntity(ITrsBillingBayarKey key)
    {
        _trsBilling2GenDal.Delete(key);
    }

    public IEnumerable<TrsBilling2Model> ListData(IRegKey regKey)
    {
        var listDto = _trsBilling2GenDal.ListData(regKey);
        if (listDto is null)
            return Enumerable.Empty<TrsBilling2Model>();

        var result = listDto.Select(dto => dto.ToModel());
        return result;
    }
}
