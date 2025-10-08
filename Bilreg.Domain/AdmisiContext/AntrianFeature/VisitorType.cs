using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record VisitorType(string VisitorName, DateOnly TglLahir, DateTime VisitDate)
{
    public static VisitorType Create(string name, DateOnly tglLahir)
        => new(name, tglLahir, DateTime.Now);

    public static VisitorType Default 
        => new("-", DateOnly.FromDateTime(new DateTime(3000,1,1)), new DateTime(3000,1,1));
}
