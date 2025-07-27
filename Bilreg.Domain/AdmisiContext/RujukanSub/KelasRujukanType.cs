using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.RujukanSub;

public record KelasRujukanType : IKelasRujukanKey
{
    public KelasRujukanType(string kelasRujukanId, string kelasRujukanName, int nilai)
    {
        Guard.Against.NullOrWhiteSpace(kelasRujukanId, nameof(kelasRujukanId));
        Guard.Against.NullOrWhiteSpace(kelasRujukanName, nameof(kelasRujukanName));

        KelasRujukanId = kelasRujukanId;
        KelasRujukanName = kelasRujukanName;
        Nilai = nilai;
    }
    
    public string KelasRujukanId { get; init; }
    public string KelasRujukanName { get; init; }
    public int Nilai { get; init; }
    
    public KelasRujukanReff ToReff() => new(KelasRujukanId, KelasRujukanName);
    
    public static KelasRujukanType Default => new("-", "-", 0);
    public static IKelasRujukanKey Key(string id) => Default with { KelasRujukanId = id };
}

public interface IKelasRujukanKey
{
    string KelasRujukanId {get;}
}

public record KelasRujukanReff(string KelasRujukanId, string KelasRujukanName);