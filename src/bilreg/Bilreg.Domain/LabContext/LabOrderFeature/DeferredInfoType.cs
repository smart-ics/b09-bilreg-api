namespace Bilreg.Domain.LabContext.LabOrderFeature;

public record DeferredInfoType(string Reason, DateTime Until)
{
    private static readonly DateTime EmptyUntil = new(3000, 1, 1);

    public static DeferredInfoType Default => new("", EmptyUntil);

    public bool IsEmpty => string.IsNullOrWhiteSpace(Reason) && Until == EmptyUntil;
}
