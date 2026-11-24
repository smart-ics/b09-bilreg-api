using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;

namespace Bilreg.Application.IgdContext.IgdVisitFeature;

public interface IIgdBillingPostingOrchestrator
{
    Task<bool> PostTindakanAsync(
        IgdVisitModel visit,
        RegModel reg,
        TindakanIgdModel tindakan,
        CancellationToken cancellationToken = default);

    Task<bool> PostBhpAsync(
        IgdVisitModel visit,
        RegModel reg,
        BhpIgdModel bhp,
        CancellationToken cancellationToken = default);

    Task<bool> SyncAllPendingForVisitAsync(
        IgdVisitModel visit,
        RegModel reg,
        CancellationToken cancellationToken = default);
}
