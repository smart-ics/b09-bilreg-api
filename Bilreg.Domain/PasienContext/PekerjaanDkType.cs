using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext;

public record PekerjaanDkType : IPekerjaanDkKey
{
    public PekerjaanDkType(string pekerjaanDkId, string pekerjaanDkName)
    {
        Guard.Against.NullOrWhiteSpace(pekerjaanDkId, nameof(pekerjaanDkId));
        Guard.Against.NullOrWhiteSpace(pekerjaanDkName, nameof(pekerjaanDkName));

        PekerjaanDkId = pekerjaanDkId;
        PekerjaanDkName = pekerjaanDkName;
    }
    
    public string PekerjaanDkId { get; init; }
    public string PekerjaanDkName { get; init; }
    
    public static IPekerjaanDkKey Key(string id) => new PekerjaanDkType(id, "-");
    public static PekerjaanDkType Default => new("-", "-");
}

public interface IPekerjaanDkKey
{
    string PekerjaanDkId {get;}
}