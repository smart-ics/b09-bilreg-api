using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.LayananFeature;

public record LayananDkType : ILayananDkKey
{
    public LayananDkType(string layananDkId, string layananDkName,
        int rawatInapCode, int rawatJalanCode, int kesehatanJiwaCode, 
        int bedahCode, int rujukanCode, int kunjunganRumahCode, 
        int layananSubCode)
    {
        
        LayananDkId = layananDkId;
        LayananDkName = layananDkName;
        RawatInapCode = rawatInapCode;
        RawatJalanCode = rawatJalanCode;
        KesehatanJiwaCode = kesehatanJiwaCode;
        BedahCode = bedahCode;
        RujukanCode = rujukanCode;
        KunjunganRumahCode = kunjunganRumahCode;
        LayananSubCode = layananSubCode;
    }
    
    public string LayananDkId { get; init; }
    public string LayananDkName { get; init; }
    public int RawatInapCode { get; init; }
    public int RawatJalanCode { get; init; }
    public int KesehatanJiwaCode { get; init; }
    public int BedahCode { get; init; }
    public int RujukanCode { get; init; }
    public int KunjunganRumahCode { get; init; }
    public int LayananSubCode { get; init; }
    
    public LayananDkReff ToReff() => new(LayananDkId, LayananDkName);
    
    public static LayananDkType Default => new("-", "-",0,0,0,0,0,0,0);
    
    public static ILayananDkKey Key(string id) => Default with { LayananDkId = id };
}

public interface ILayananDkKey
{
    string LayananDkId {get;}
}

public record LayananDkReff(string LayananDkId, string LayananDkName);