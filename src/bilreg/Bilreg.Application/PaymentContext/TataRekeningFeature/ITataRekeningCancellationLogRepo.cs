using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

public interface ITataRekeningCancellationLogRepo :
    ISaveChange<TataRekeningCancellationLog>
{
}
