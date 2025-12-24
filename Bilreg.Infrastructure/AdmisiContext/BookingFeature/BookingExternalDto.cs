using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;


namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public record BookingExternalDto(
    string BookingId, string ExtAppName, string ReffId, string CheckInQr)
{
    public static BookingExternalDto FromModel(string BookingId,ExtAppReffType model)
    {
        var result = new BookingExternalDto(BookingId, 
            model.ExtAppName, model.ReffId, model.CheckInQr);
        return result;
    
    }
}

public record BookingExtDto(string BookingId, DateTime BookingDate, DateTime TglBerobat, 
    string DokterId, string RegId, string PasienId, string PasienName, DateTime TglLahir, 
    string Alamat, string Gender, string LayananId, string JamPraktek, int NoAntrian,
    string TelpPasien, string AsuransiName, string NoPeserta, string NoRujukan,
    string ExtAppName, string ReffId, string CheckInQr,
     string DokterName, string LayananName)
{
    public BookingExtView ToView()
    {
        var lyn = new LayananReff(LayananId, LayananName);
        var reg = new RegReff(RegId, PasienId, PasienName);
        var dokter = new PpaReff(DokterId, DokterName);
        var extApp = new ExtAppReffType(ExtAppName, ReffId, CheckInQr);
        var coverage = new CoverageInfoType(AsuransiName, NoPeserta, NoRujukan);

        var tglLahir = DateOnly.FromDateTime(TglLahir);
        var alamat = new AlamatType([Alamat], "-", "-");
        var contact = new ContactType(JenisContactEnum.Phone, TelpPasien);
        var person = new PersonInfoType(
            PasienName, tglLahir, Gender, alamat,
            contact, IdentitasType.Default);

        var result = new BookingExtView(BookingId, BookingDate, TglBerobat, 
            person,reg, lyn, dokter, TimeOnly.Parse(JamPraktek), NoAntrian,  
            extApp, coverage);
        return result;
    }
}