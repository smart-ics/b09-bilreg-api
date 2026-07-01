namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public record JenisOperasiType(string JenisOperasiId, string JenisOperasiName) : IJenisOperasiKey
{
    public static JenisOperasiType Default => new JenisOperasiType("-", "-");
    public static IJenisOperasiKey Key(string id) => new JenisOperasiType(id, "-");
}

public interface IJenisOperasiKey
{
    string JenisOperasiId { get; }
}