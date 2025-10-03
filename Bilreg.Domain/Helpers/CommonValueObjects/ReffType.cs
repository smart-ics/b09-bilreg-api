namespace Emr25.Domain.HelpersContext.CommonValueObjects;

public record ReffType(string ReffId, string ReffKind)
{
    public static ReffType Default => new ReffType(string.Empty, string.Empty);
};