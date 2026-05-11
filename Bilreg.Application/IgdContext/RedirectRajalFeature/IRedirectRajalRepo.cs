using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.RedirectRajalFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.IgdContext.RedirectRajalFeature;

public interface IRedirectRajalRepo :
    ISaveChange<RedirectRajalModel>,
    ILoadEntity<RedirectRajalModel, IRedirectRajalKey>,
    IListData<RedirectRajalView, IIgdVisitKey>
{
}

public record RedirectRajalView(
    string RedirectRajalId,
    string IgdVisitId,
    string VisitorName,
    DateTime RedirectDateTime,
    string Reason,
    string RedirectUserId);
