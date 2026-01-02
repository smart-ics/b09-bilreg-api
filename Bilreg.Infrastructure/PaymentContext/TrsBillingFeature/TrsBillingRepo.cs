using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public class TrsBillingRepo : ITrsBillingRepo
 {
     private readonly ITrsBillingDal _billingDal;
     private readonly ITrsBilling2Dal _billing2Dal;
     public TrsBillingRepo(ITrsBillingDal billingDal, ITrsBilling2Dal billing2Dal)
     {
         _billingDal = billingDal;
         _billing2Dal = billing2Dal;
     }

     public void SaveChanges(TrsBillingType model)
     {
         LoadEntity(model)
             .Match(
                 onSome: x => _billingDal.Update(TrsBillingDto.FromModel(model)),
                 onNone: () => _billingDal.Insert(TrsBillingDto.FromModel(model)));

        var listBilling2Dto = model.ListTrsBilling2
                .Select(x => TaTrsBilling2Dto.FromModel(x,model.TrsBillingId));
        _billing2Dal.Delete(model);
        _billing2Dal.Insert(listBilling2Dto);
     }

     public MayBe<TrsBillingType> LoadEntity(ITrsBillingKey key)
     {
         var data = _billingDal.GetData(key);
         if (data is null)
             return MayBe<TrsBillingType>.None;

         var listKomp = _billing2Dal.ListData(key);
         var result = data.ToModel(listKomp.Select(x => x.ToModel()));

         return MayBe.From(result);
     }
     public void DeleteEntity(ITrsBillingKey key)
     {
         _billingDal.Delete(key);
         _billing2Dal.Delete(key);
     }
 }