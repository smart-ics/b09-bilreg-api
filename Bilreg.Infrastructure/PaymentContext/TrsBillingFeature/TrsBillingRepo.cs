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
         var data = _billingDal.GetData(key);
         if (data is null)
             return MayBe<TrsBillType>.None;
         
         var listBill2Dto = _billing2Dal.ListData(key)?.ToList() ?? [];
         var list2Bill = listBill2Dto.Select(x => x.ToModel((int)data.fn_modul)).ToList();
         var result = data.ToModel(list2Bill);
         
         return MayBe.From(result);
     }
     public void DeleteEntity(ITrsBillingKey key)
     {
         _billingDal.Delete(key);
         _billing2Dal.Delete(key);
     }

    public IEnumerable<TrsBillView> ListData(IRegKey regKey)
    {
        var listDto = _billingDal.ListData(regKey)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToView()).ToList();
        return result;
    }

    public IEnumerable<TrsBillType> ListEntity(IRegKey regKey)
    {
        var headers = _billingDal.ListData(regKey)?.ToList() ?? [];
        if (headers.Count == 0)
            return [];

        var allBill2 = _billing2Dal.ListData(regKey)?.ToList() ?? [];
        var bill2ById = allBill2
            .GroupBy(x => x.fs_kd_trs)
            .ToDictionary(g => g.Key, g => g.ToList());

        return headers.Select(header =>
        {
            var children = bill2ById.GetValueOrDefault(header.fs_kd_trs, []);
            var events = children.Select(x => x.ToModel((int)header.fn_modul)).ToList();
            return header.ToModel(events);
        });
    }

}