using Bilreg.Application.PaymentContext.PasienBalanceFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public class PasienBalanceRepo : IPasienBalanceRepo
{
    private readonly IBilrgTataRekPasienBalanceDal _headerDal;
    private readonly IBilrgTataRekPasienBalanceHistoryDal _historyDal;

    public PasienBalanceRepo(
        IBilrgTataRekPasienBalanceDal headerDal,
        IBilrgTataRekPasienBalanceHistoryDal historyDal)
    {
        _headerDal = headerDal;
        _historyDal = historyDal;
    }

    public void SaveChanges(PasienBalanceModel model)
    {
        var exists = _headerDal.GetData(model) is not null;

        if (!exists)
        {
            _headerDal.Insert(BilrgTataRekPasienBalanceDto.FromModelForInsert(model));
        }
        else
        {
            var dto = BilrgTataRekPasienBalanceDto.FromModelForUpdate(model);
            var rows = _headerDal.UpdateConditional(dto, model.Version);
            if (rows == 0)
                throw new InvalidOperationException(
                    $"PasienBalance {model.PasienId} stale; please reload (expected version: {model.Version}).");

            model.CommitVersionIncrement();
        }

        var pendingHistory = model.PendingHistory
            .Select(BilrgTataRekPasienBalanceHistoryDto.FromModel)
            .ToList();

        if (pendingHistory.Count > 0)
            _historyDal.Insert(pendingHistory);
    }

    public MayBe<PasienBalanceModel> LoadEntity(IPasienKey key)
    {
        var header = _headerDal.GetData(key);
        if (header is null)
            return MayBe<PasienBalanceModel>.None;

        var history = ListHistory(key).ToList();
        return MayBe.From(header.ToModel(history));
    }

    public IEnumerable<PasienBalanceHistoryType> ListHistory(IPasienKey key)
        => (_historyDal.ListData(key)?.ToList() ?? [])
            .Select(x => x.ToModel());
}
