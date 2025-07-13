using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext;

public record PendidikanDkType : IPendidikanDkKey
{
    public PendidikanDkType(string pendidikanDkId, string pendidikanDkName)
    {
        Guard.Against.NullOrWhiteSpace(pendidikanDkId, nameof(pendidikanDkId));
        Guard.Against.NullOrWhiteSpace(pendidikanDkName, nameof(pendidikanDkName));

        PendidikanDkId = pendidikanDkId;
        PendidikanDkName = pendidikanDkName;
    }
    
    public string PendidikanDkId { get; init; }
    public string PendidikanDkName { get; init; }
    
    public static IPendidikanDkKey Key(string id) => new PendidikanDkType(id, "-");
    public static PendidikanDkType Default => new("-", "-");
}

public interface IPendidikanDkKey
{
    string PendidikanDkId {get;}
}