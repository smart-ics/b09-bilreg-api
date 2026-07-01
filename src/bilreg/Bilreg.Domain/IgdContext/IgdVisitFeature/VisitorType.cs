namespace Bilreg.Domain.IgdContext.IgdVisitFeature;

public record VisitorType(
    string VisitorName,
    string Gender,
    DateOnly TglLahir,
    string Kontak)
{
    public static VisitorType Default => new(
        VisitorName: "-",
        Gender: "-",
        TglLahir: new DateOnly(3000, 1, 1),
        Kontak: "-");
}
