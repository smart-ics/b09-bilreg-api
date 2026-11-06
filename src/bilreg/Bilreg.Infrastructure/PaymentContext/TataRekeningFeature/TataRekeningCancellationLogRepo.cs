using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public class TataRekeningCancellationLogRepo : ITataRekeningCancellationLogRepo
{
    private readonly ITataRekeningCancellationLogDal _cancellationLogDal;

    public TataRekeningCancellationLogRepo(ITataRekeningCancellationLogDal cancellationLogDal)
    {
        _cancellationLogDal = cancellationLogDal;
    }

    public void SaveChanges(TataRekeningCancellationLog model)
    {
        _cancellationLogDal.Insert(TataRekeningCancellationLogDto.FromModel(model));
    }
}
