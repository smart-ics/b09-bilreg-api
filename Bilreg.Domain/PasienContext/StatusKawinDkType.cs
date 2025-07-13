using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext;

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
    
    public static IStatusKawinDkKey Key(string id) => new StatusKawinDkType(id, "-");
    public static StatusKawinDkType Default => new("-", "-");
}

public interface IStatusKawinDkKey
{
    string StatusKawinDkId {get;}
}