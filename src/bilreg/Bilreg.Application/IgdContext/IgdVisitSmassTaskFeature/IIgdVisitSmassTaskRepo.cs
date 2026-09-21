using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;

public interface IIgdVisitSmassTaskRepo :
    ISaveChange<IgdVisitSmassTaskModel>,
    ILoadEntity<IgdVisitSmassTaskModel, IIgdVisitSmassTaskKey>
{
    /// <summary>
    /// Idempotent upsert lookup on the business key (IgdVisitId, NoTriage, TaskType) — INV-T1.
    /// </summary>
    MayBe<IgdVisitSmassTaskModel> FindByBusinessKey(
        string igdVisitId,
        int noTriage,
        SmassTaskTypeEnum taskType);

    IEnumerable<IgdVisitSmassTaskModel> ListByVisit(string igdVisitId);

    /// <summary>
    /// Operator worklist: Failed tasks across visits, ordered by CrtDate ascending.
    /// </summary>
    IEnumerable<IgdVisitSmassTaskModel> ListProcessable();
}
