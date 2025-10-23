using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.SearchPasienSub;

public class SearchPasienDto
{
    public string PasienId { get; set; }
    public string PasienName { get; set; }
    public string TglLahir { get; set; }
    public string Gender { get; set; }

    public string JenisId { get; set; }
    public string NoId { get; set; }
    public string IbuKandung { get; set; }
    public string AlamatPasien { get; set; }
    public string Kota { get; set; }
    public string KodePos { get; set; }
    public string RegId { get; set; }
    public string BookingId { get; set; }

    public SearchPasienType ToModel()
    {
        var alamat = new AlamatType([AlamatPasien], Kota, KodePos);
        var identitas = new IdentitasType(JenisId, NoId);
        
        var pasien = new SearchPasienType(PasienId, PasienName, TglLahir.ToDate("yyyy-MM-dd"),
            Gender, identitas, IbuKandung, alamat, RegId, BookingId);
        return pasien;

    }
}

public class SearcQuickhPasienDto
{
    public string PasienId { get; set; }
    public string PasienName { get; set; }
    public DateTime TglLahir { get; set; }
    public string Gender { get; set; }

    public string JenisId { get; set; }
    public string NoId { get; set; }
    public string IbuKandung { get; set; }
    public string AlamatPasien { get; set; }
    public string Kota { get; set; }
    public string KodePos { get; set; }
    public string RegId { get; set; }
    public string BookingId { get; set; }

    public SearchPasienType ToModel()
    {
        var alamat = new AlamatType([AlamatPasien], Kota, KodePos);
        var identitas = new IdentitasType(JenisId, NoId);

        var pasien = new SearchPasienType(PasienId, PasienName, TglLahir,
            Gender, identitas, IbuKandung, alamat, RegId, BookingId);
        return pasien;

    }
}
