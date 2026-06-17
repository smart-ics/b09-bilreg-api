using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
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

     public void SaveChanges(TrsBillType model)
     {
        LoadEntity(model)
             .Match(
                 onSome: x => _billingDal.Update(TrsBillingDto.FromModel(model)),
                 onNone: () => _billingDal.Insert(TrsBillingDto.FromModel(model)));

         
        var listBillTrans = model.ListTransaction
            .Select(x => TaTrsBilling2Dto.FromModelTrans(x, model.TrsBillingId, (int)model.ModulGroup));
        var listBillDischarge = model.ListDischarge
            .Select(x => TaTrsBilling2Dto.FromModelDischarge(x, model.TrsBillingId, (int)model.ModulGroup, model.Reg.RegId));
        var listBillPayment = model.ListPayment
            .SelectMany(x =>
            {
                var (resultP, resultN) = TaTrsBilling2Dto.FromModelPayment(
                    x, model.TrsBillingId, (int)model.ModulGroup, x.Payment.PaymentId);
                return new[] { resultP, resultN };
            });
        
        //Combine all
        var listBillAll = listBillTrans
            .Union(listBillDischarge)
            .Union(listBillPayment)
            .ToList();
        
        _billing2Dal.Delete(model);
        _billing2Dal.Insert(listBillAll);
     }

     public MayBe<TrsBillType> LoadEntity(ITrsBillingKey key)
     {
         // var data = _billingDal.GetData(key);
         // if (data is null)
         //     return MayBe<TrsBillingType>.None;
         //
         // var listKomp = _billing2Dal.ListData(key);
         // var result = data.ToModel(listKomp.Select(x => x.ToModel()));
         //
         // return MayBe.From(result);
         throw new NotImplementedException();
     }
     public void DeleteEntity(ITrsBillingKey key)
     {
         _billingDal.Delete(key);
         _billing2Dal.Delete(key);
     }

    public IEnumerable<TrsBillType> ListData(IRegKey regKey)
    {
        // var listDto = _billingDal.ListData(regKey);
        // if (listDto is null)
        //     return Enumerable.Empty<TrsBillingType>();
        //
        // var result = new List<TrsBillingType>();
        // foreach (var dto in listDto)
        // {
        //     var key = TrsBillingType.Key(dto.fs_kd_trs);
        //     var listKomp = _billing2Dal.ListData(key);
        //     var entity = dto.ToModel(listKomp?.Select(x => x.ToModel()) ?? Enumerable.Empty<TrsBilling2Base>());
        //     result.Add(entity);
        // }
        //
        // return result;
        throw new NotImplementedException();
    }

}