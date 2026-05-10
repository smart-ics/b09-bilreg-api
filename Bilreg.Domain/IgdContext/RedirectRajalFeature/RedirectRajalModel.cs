using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.IgdContext.RedirectRajalFeature;

public class RedirectRajalModel : IRedirectRajalKey
{
    private const string ID_PREFIX = "RDR";

    #region CREATION
    public RedirectRajalModel(
        string redirectRajalId,
        string igdVisitId,
        string visitorName,
        DateTime redirectDateTime,
        string reason,
        string redirectUserId)
    {
        RedirectRajalId = redirectRajalId;
        IgdVisitId = igdVisitId;
        VisitorName = visitorName;
        RedirectDateTime = redirectDateTime;
        Reason = reason;
        RedirectUserId = redirectUserId;
    }

    public static RedirectRajalModel Default => new(
        redirectRajalId: "-",
        igdVisitId: "-",
        visitorName: "-",
        redirectDateTime: new DateTime(3000, 1, 1),
        reason: "-",
        redirectUserId: "-");

    public static IRedirectRajalKey Key(string id) => new RedirectRajalModel(
        redirectRajalId: id,
        igdVisitId: "-",
        visitorName: "-",
        redirectDateTime: new DateTime(3000, 1, 1),
        reason: "-",
        redirectUserId: "-");

    public static RedirectRajalModel Create(IgdVisitModel visit, string reason, AuditInfoType audit)
    {
        Guard.Against.Null(visit);
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        return new RedirectRajalModel(
            redirectRajalId: NunaId.New(ID_PREFIX),
            igdVisitId: visit.IgdVisitId,
            visitorName: visit.Visitor.VisitorName,
            redirectDateTime: audit.Timestamp,
            reason: string.IsNullOrWhiteSpace(reason) ? "-" : reason,
            redirectUserId: audit.UserId);
    }
    #endregion

    #region PROPERTIES
    public string RedirectRajalId { get; init; }
    public string IgdVisitId { get; init; }
    public string VisitorName { get; init; }
    public DateTime RedirectDateTime { get; init; }
    public string Reason { get; init; }
    public string RedirectUserId { get; init; }
    #endregion
}
