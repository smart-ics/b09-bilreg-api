namespace Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;

public interface IJaminanKey
{
    string JaminanId { get; }
}

public record JaminanKey(string JaminanId) : IJaminanKey;