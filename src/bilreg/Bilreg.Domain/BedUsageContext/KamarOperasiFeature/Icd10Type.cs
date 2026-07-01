namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public record Icd10Type(string Icd10Id, string Icd10Name) : IIcd10Key
{
    public static Icd10Type Default => new Icd10Type("-", "-");
    public static IIcd10Key Key(string id) => Default with { Icd10Id = id };
}

public interface IIcd10Key
{
    string Icd10Id { get; }
}