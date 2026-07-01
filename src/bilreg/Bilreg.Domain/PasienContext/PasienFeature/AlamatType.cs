using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.DemografiFeature;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public record AlamatType
{
    public AlamatType(string[] alamat, string kota, string kodePos)
    {
        Guard.Against.Null(alamat, nameof(alamat));
        
        Guard.Against.Null(kota, nameof(kota));
        Guard.Against.OutOfRange(kota.Length, nameof(kota), 0, 30, "Kota maksimal 30 karakter");

        Guard.Against.Null(kodePos, nameof(kodePos));

        Alamat = alamat;
        Kota = kota;
        KodePos = kodePos;
    }

    public string[] Alamat { get; init; }
    public string Kota { get; init; }
    public string KodePos { get; init; }
    
    public static AlamatType Default => new AlamatType(["-", "-", "-"],  "-", "-");
}
