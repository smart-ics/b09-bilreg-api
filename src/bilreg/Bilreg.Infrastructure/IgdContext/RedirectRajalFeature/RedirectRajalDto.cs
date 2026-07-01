using Bilreg.Application.IgdContext.RedirectRajalFeature;
using Bilreg.Domain.IgdContext.RedirectRajalFeature;

namespace Bilreg.Infrastructure.IgdContext.RedirectRajalFeature;

public record RedirectRajalDto(
    string RedirectRajalId,
    string IgdVisitId,
    string VisitorName,
    DateTime RedirectDateTime,
    string Reason,
    string RedirectUserId)
{
    public static RedirectRajalDto FromModel(RedirectRajalModel model)
        => new(
            RedirectRajalId: model.RedirectRajalId,
            IgdVisitId: model.IgdVisitId,
            VisitorName: model.VisitorName,
            RedirectDateTime: model.RedirectDateTime,
            Reason: model.Reason,
            RedirectUserId: model.RedirectUserId);

    public RedirectRajalModel ToModel()
        => new(
            redirectRajalId: RedirectRajalId,
            igdVisitId: IgdVisitId,
            visitorName: VisitorName,
            redirectDateTime: RedirectDateTime,
            reason: Reason,
            redirectUserId: RedirectUserId);

    public RedirectRajalView ToView()
        => new(
            RedirectRajalId: RedirectRajalId,
            IgdVisitId: IgdVisitId,
            VisitorName: VisitorName,
            RedirectDateTime: RedirectDateTime,
            Reason: Reason,
            RedirectUserId: RedirectUserId);
}
