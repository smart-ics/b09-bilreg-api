using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.LayananSub;

public record LayananDkType : ILayananDkKey
{
    public LayananDkType(string layananDkId, string layananDkName,
        int rawatInapCode = 0, int rawatJalanCode = 0, int kesehatanJiwaCode = 0, 
        int bedahCode = 0, int rujukanCode = 0, int kunjunganRumahCode = 0, 
        int layananSubCode = 0)
    {
        Guard.Against.NullOrWhiteSpace(layananDkId, nameof(layananDkId));
        Guard.Against.NullOrWhiteSpace(layananDkName, nameof(layananDkName));

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
    public int RawatInapCode { get; protected set; }
    public int RawatJalanCode { get; protected set; }
    public int KesehatanJiwaCode { get; protected set; }
    public int BedahCode { get; protected set; }
    public int RujukanCode { get; protected set; }
    public int KunjunganRumahCode { get; protected set; }
    public int LayananSubCode { get; protected set; }
    
    public LayananDkReff ToReff() => new(LayananDkId, LayananDkName);
    
    public static LayananDkType Default => new("-", "-",0,0,0,0,0,0,0);
    
    public static ILayananDkKey Key(string id) => Default with { LayananDkId = id };
}

public interface ILayananDkKey
{
    string LayananDkId {get;}
}

public record LayananDkReff(string LayananDkId, string LayananDkName);