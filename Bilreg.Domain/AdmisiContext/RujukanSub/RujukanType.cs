using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext;

namespace Bilreg.Domain.AdmisiContext.RujukanSub;

public record RujukanType : IRujukanKey
{
    public RujukanType(string rujukanId, string rujukanName)
    {
        Guard.Against.NullOrWhiteSpace(rujukanId, nameof(rujukanId));
        Guard.Against.NullOrWhiteSpace(rujukanName, nameof(rujukanName));

        RujukanId = rujukanId;
        RujukanName = rujukanName;
    }
    
    public string RujukanId { get; init; }
    public string RujukanName { get; init; }
    public bool IsAktif { get; init; }
    public AlamatType Alamat { get; init; }
    public TipeRujukanType TipeRujukan { get; init; }
    public KelasRujukanReff KelasRujukan { get; init; }
    public CaraMasukDkType CaraMasukDk { get; init; }
    
    public static IRujukanKey Key(string id) => new RujukanType(id, "-");
    public static RujukanType Default => new("-", "-");
}

public interface IRujukanKey
{
    string RujukanId {get;}
}