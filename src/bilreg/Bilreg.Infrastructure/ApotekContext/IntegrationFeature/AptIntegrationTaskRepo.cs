using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ApotekContext.IntegrationFeature;

public class AptIntegrationTaskRepo : IAptIntegrationTaskRepo
{
    private readonly IAptIntegrationTaskDal _dal;

    public AptIntegrationTaskRepo(IAptIntegrationTaskDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(AptIntegrationTaskModel model)
    {
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(model);
        else
            _dal.Update(model);
    }

    public MayBe<AptIntegrationTaskModel> LoadEntity(IAptIntegrationTaskKey key)
    {
        var model = _dal.GetData(key);
        return model is null ? MayBe<AptIntegrationTaskModel>.None : MayBe.From(model);
    }

    public MayBe<AptIntegrationTaskModel> LoadByIdempotencyKey(string idempotencyKey)
    {
        var model = _dal.GetByIdempotencyKey(idempotencyKey);
        return model is null ? MayBe<AptIntegrationTaskModel>.None : MayBe.From(model);
    }

    public IEnumerable<AptIntegrationTaskModel> ListPending(int batchSize)
        => _dal.ListPending(batchSize);

    public bool ClaimPending(IAptIntegrationTaskKey key)
        => _dal.ClaimPending(key);
}
