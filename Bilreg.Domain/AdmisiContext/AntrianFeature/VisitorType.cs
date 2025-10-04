using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record VisitorType(string VisitorId, string VisitorName, DateTime TglLahir, string RegId) : IRegKey
{
    public static VisitorType Default => new("-", "-", new DateTime(3000,1,1), "-");
}
