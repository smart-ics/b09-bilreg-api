using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.StatusSosialFeature;

public record StatusKawinDkType : IStatusKawinDkKey
{
    public StatusKawinDkType(string statusKawinDkId, string statusKawinDkName)
    {
        Guard.Against.NullOrWhiteSpace(statusKawinDkId, nameof(statusKawinDkId));
        Guard.Against.NullOrWhiteSpace(statusKawinDkName, nameof(statusKawinDkName));

        StatusKawinDkId = statusKawinDkId;
        StatusKawinDkName = statusKawinDkName;
    }
    
    public string StatusKawinDkId { get; init; }
    public string StatusKawinDkName { get; init; }
    
    public static StatusKawinDkType Default => new("-", "-");
    public static IStatusKawinDkKey Key(string id) => Default with { StatusKawinDkId = id };
}

public interface IStatusKawinDkKey
{
    string StatusKawinDkId {get;}
}
