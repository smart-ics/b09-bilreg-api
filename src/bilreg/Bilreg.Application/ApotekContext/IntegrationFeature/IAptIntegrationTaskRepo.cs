using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.IntegrationFeature;

public interface IAptIntegrationTaskRepo :
    ISaveChange<AptIntegrationTaskModel>,
    ILoadEntity<AptIntegrationTaskModel, IAptIntegrationTaskKey>
{
    MayBe<AptIntegrationTaskModel> LoadByIdempotencyKey(string idempotencyKey);
    IEnumerable<AptIntegrationTaskModel> ListPending(int batchSize);
    bool ClaimPending(IAptIntegrationTaskKey key);
}
