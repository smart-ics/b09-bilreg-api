using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ApotekContext.IntegrationFeature;

public interface IAptIntegrationTaskDal :
    IInsert<AptIntegrationTaskModel>,
    IUpdate<AptIntegrationTaskModel>,
    IGetData<AptIntegrationTaskModel, IAptIntegrationTaskKey>
{
    AptIntegrationTaskModel? GetByIdempotencyKey(string idempotencyKey);
    IEnumerable<AptIntegrationTaskModel> ListPending(int batchSize);
    bool ClaimPending(IAptIntegrationTaskKey key);
}
