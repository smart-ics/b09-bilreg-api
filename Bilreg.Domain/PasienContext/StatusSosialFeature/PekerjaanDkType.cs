using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.StatusSosialFeature;

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
    
    public static PekerjaanDkType Default => new("-", "-");
    public static IPekerjaanDkKey Key(string id) => Default with { PekerjaanDkId = id };
}

public interface IPekerjaanDkKey
{
    string PekerjaanDkId {get;}
}