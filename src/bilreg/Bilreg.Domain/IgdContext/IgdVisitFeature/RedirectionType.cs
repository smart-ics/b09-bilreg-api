namespace Bilreg.Domain.IgdContext.IgdVisitFeature;

public record RedirectionType(
    string RedirectRajalId,
    DateTime RedirectDateTime,
    string Reason)
{
    public static RedirectionType Default => new(
        RedirectRajalId: "-",
        RedirectDateTime: new DateTime(3000, 1, 1),
        Reason: "-");
}
