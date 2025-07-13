using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext;

public record TipeRujukanType : ITipeRujukanKey
{
    public TipeRujukanType(string tipeRujukanId, string tipeRujukanName)
    {
        Guard.Against.NullOrWhiteSpace(tipeRujukanId, nameof(tipeRujukanId));
        Guard.Against.NullOrWhiteSpace(tipeRujukanName, nameof(tipeRujukanName));

        TipeRujukanId = tipeRujukanId;
        TipeRujukanName = tipeRujukanName;
    }
    
    public string TipeRujukanId { get; init; }
    public string TipeRujukanName { get; init; }
    
    public static ITipeRujukanKey Key(string id) => new TipeRujukanType(id, "-");
    public static TipeRujukanType Default => new("-", "-");
}

public interface ITipeRujukanKey
{
    string TipeRujukanId {get;}
}