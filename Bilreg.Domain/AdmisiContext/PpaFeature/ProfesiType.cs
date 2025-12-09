using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.PpaFeature;

public record ProfesiType : IProfesiKey
{
    #region CREATION
    public ProfesiType(string profesiId, string profesiName)
    {
        ProfesiId = profesiId;
        ProfesiName = profesiName;
    }
    public static ProfesiType Create(string profesiId, string profesiName)
    {
        Guard.Against.NullOrWhiteSpace(profesiId, nameof(profesiId));
        Guard.Against.NullOrWhiteSpace(profesiName, nameof(profesiName));
        return new ProfesiType(profesiId, profesiName);
    }
    public static ProfesiType Default => new("-", "-");
    public static IProfesiKey Key(string id) => Default with { ProfesiId = id };
    public static ProfesiType Dokter => new("DOK", "Dokter");
    public static ProfesiType Perawat => new("PRW", "Perawat");
    #endregion
    
    #region PROPERTIES
    public string ProfesiId { get; init; }
    public string ProfesiName { get; init; }
    #endregion
}

public interface IProfesiKey
{
    string ProfesiId {get;}
}
