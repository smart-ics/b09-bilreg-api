using Bilreg.Domain.PasienContext.DemografiFeature;

namespace Bilreg.Domain.PasienContext.PasienFeature;

// public record KtpType (
//     string Nik, string Nama,
//     string TempatLahir, string TglLahir,
//     string Gender,string GolDarah,
//     string Alamat, string Rt, string Rw,
//     KelurahanType Kelurahan)
// {
// };

public record KtpType
{
    public KtpType(string nik, AlamatType alamat, string rt, string rw, 
        KelurahanReff kelurahan)
    {
        Nik = nik;
        Alamat = alamat;
        Rt = rt;
        Rw = rw;
        Kelurahan = kelurahan;
    }
    public string Nik { get; init; }
    public AlamatType Alamat { get; init; }
    public string Rt { get; init; }
    public string Rw { get; init; }
    public KelurahanReff Kelurahan { get; init; }
}