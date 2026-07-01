using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public class TataRekeningRepo : ITataRekeningRepo
{
    private readonly IBilrgTataRekeningDal _headerDal;
    private readonly ITaRegistrasi3Dal _paymentDal;
    private readonly ITrsBillingRepo _trsBillingRepo;

    public TataRekeningRepo(
        IBilrgTataRekeningDal headerDal,
        ITaRegistrasi3Dal paymentDal,
        ITrsBillingRepo trsBillingRepo)
    {
        _headerDal = headerDal;
        _paymentDal = paymentDal;
        _trsBillingRepo = trsBillingRepo;
    }

    public void SaveChanges(TataRekeningModel model)
    {
        var exists = LoadEntity(model).HasValue;

        if (exists)
        {
            var dto = BilrgTataRekeningDto.FromModel(model);
            var rows = _headerDal.UpdateConditional(dto, model.Version);
            if (rows == 0)
                throw new InvalidOperationException(
                    $"Tata Rekening '{model.RegId}' stale; please reload (expected version: {model.Version}).");

            model.CommitVersionIncrement();
        }
        else
        {
            _headerDal.Insert(BilrgTataRekeningDto.FromModelForInsert(model));
            model.CommitVersionIncrement();
        }

        _paymentDal.Delete(model);
        var paymentRows = model.ListPayment
            .Select(p => TaRegistrasi3Dto.FromModel(model.RegId, p))
            .ToList();
        _paymentDal.Insert(paymentRows);
    }

    public MayBe<TataRekeningModel> LoadEntity(IRegKey key)
    {
        var header = _headerDal.GetData(key);
        if (header is null)
            return MayBe<TataRekeningModel>.None;

        var (status, finalizationInfo) = header.ToHeaderParts();
        var (finVerifStatus, finVerifInfo, isAllocated, settlementInitiated, version) = header.ToPhase1Parts();
        var payments = _paymentDal.ListData(key)?.Select(x => x.ToModel()).ToList() ?? [];
        var bills = _trsBillingRepo.ListEntity(key).ToList();

        return MayBe.From(new TataRekeningModel(
            header.RegId,
            status,
            finalizationInfo,
            payments,
            bills,
            finVerifStatus,
            finVerifInfo,
            isAllocated,
            settlementInitiated,
            version));
    }

    public void DeleteEntity(IRegKey key)
    {
        _headerDal.Delete(key);
        _paymentDal.Delete(key);
    }
}
