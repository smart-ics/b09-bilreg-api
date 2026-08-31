using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.IntegrationFeature;

public static class AptIntegrationTaskEnqueue
{
    public static void InsertIfAbsent(IAptIntegrationTaskRepo repo, AptIntegrationTaskModel task)
    {
        var existing = repo.LoadByIdempotencyKey(task.IdempotencyKey);
        if (existing.HasValue)
            return;
        repo.SaveChanges(task);
    }

    public static bool RequireAmbientTransaction()
        => true;
}
