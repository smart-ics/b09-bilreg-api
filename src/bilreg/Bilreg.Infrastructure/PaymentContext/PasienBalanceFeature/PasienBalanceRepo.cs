using Bilreg.Application.PaymentContext.PasienBalanceFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public class PasienBalanceRepo : IPasienBalanceRepo
{
    private readonly IBilrgTataRekPasienBalanceDal _headerDal;
    private readonly IBilrgTataRekPasienBalanceOutstandingDal _outstandingDal;

    public PasienBalanceRepo(
        IBilrgTataRekPasienBalanceDal headerDal,
        IBilrgTataRekPasienBalanceOutstandingDal outstandingDal)
    {
        _headerDal = headerDal;
        _outstandingDal = outstandingDal;
    }

    public void SaveChanges(PasienBalanceModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ =>
                {
                    var dto = BilrgTataRekPasienBalanceDto.FromModelForUpdate(model);
                    var rows = _headerDal.UpdateConditional(dto, model.Version);
                    if (rows == 0)
                        throw new InvalidOperationException(
                            $"PasienBalance {model.PasienId} stale; please reload (expected version: {model.Version}).");

                    model.CommitVersionIncrement();
                },
                onNone: () => _headerDal.Insert(BilrgTataRekPasienBalanceDto.FromModelForInsert(model)));

        _outstandingDal.Delete(model);
        var outstandingRows = model.OutstandingEntries
            .Select(BilrgTataRekPasienBalanceOutstandingDto.FromModel)
            .ToList();
        _outstandingDal.Insert(outstandingRows);
    }

    public MayBe<PasienBalanceModel> LoadEntity(IPasienKey key)
    {
        var header = _headerDal.GetData(key);
        if (header is null)
            return MayBe<PasienBalanceModel>.None;

        var entries = (_outstandingDal.ListData(key)?.ToList() ?? [])
            .Select(x => x.ToModel())
            .ToList();

        return MayBe.From(header.ToModel(entries));
    }
}
