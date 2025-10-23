namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record PersonType(string PersonName, DateOnly TglLahir)
{
    public static PersonType Default => new("", new DateOnly(3000,1,1));
}