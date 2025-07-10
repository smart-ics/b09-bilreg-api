using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;

public interface IGrupJaminanKey
{
    string GrupJaminanId { get; }
}

public record GrupJaminanKey : IGrupJaminanKey
{
    public GrupJaminanKey(string grupJaminanId)
    {
        Guard.IsNotNullOrWhiteSpace(grupJaminanId);
        GrupJaminanId = grupJaminanId;
    }
    public string GrupJaminanId { get; }
}