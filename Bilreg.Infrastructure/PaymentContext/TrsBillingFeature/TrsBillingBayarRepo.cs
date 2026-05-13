using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public class TrsBillingBayarRepo : ITrsBillingBayarRepo
{
    private readonly ITrsBilling2Dal _dal;
    public TrsBillingBayarRepo(ITrsBilling2Dal dal) => _dal = dal;

    public void SaveChanges(IEnumerable<TrsBilling2Base> models)
    {
        var first = models.FirstOrDefault();
        if (first is null) return;

        _dal.DeleteByPaymentId(first.PaymentId);
        var dtos = models
            .Select(m => TaTrsBilling2Dto.FromModel(m, m.TrsBillingId));
        _dal.Insert(dtos);
    }

    public IEnumerable<TrsBilling2Base> ListData(IRegKey regKey)
    {
        var listDto = _dal.ListData(regKey);
        if (listDto is null)
            return Enumerable.Empty<TrsBilling2Base>();

        var result = listDto.Select(dto => dto.ToModel());
        return result;

    }

    public void DeleteByPaymentId(string paymentId)
    {
        _dal.DeleteByPaymentId(paymentId);
    }
}