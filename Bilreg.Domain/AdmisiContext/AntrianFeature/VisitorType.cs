using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record VisitorType(string VisitorId, string VisitorName, string RegId) : IRegKey
{
    public static VisitorType Default => new("-", "-", "-");
}
