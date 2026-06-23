namespace Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;

public record RuangType :IRuangKey
{
    public RuangType(string ruangId, string ruangName, string prefixAntrian)
    {
        RuangId = ruangId;
        RuangName = ruangName;
        PrefixAntrian = prefixAntrian;
    }

    public static RuangType Default
        => new("-", "-", "-");

    public string RuangId { get; init; }
    public string RuangName { get; init; }
    public string PrefixAntrian { get; init; }
}


public interface IRuangKey
{
    string RuangId { get; }
}