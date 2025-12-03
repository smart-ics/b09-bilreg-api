using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Helpers.CommonValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public record BookingDto(
    //      main table
    string BookingId, DateTime BookingDate,     
    string PasienName, DateTime TglLahir, string Gender, string Alamat, string TelpPasien,
    string PasienId, string RegId, DateTime TglBerobat, string JamPraktek, 
    string LayananId, string DokterId, int NoAntrian,
    string NoPeserta, string NoReffKontrol, string ReffId,
    string CrtUser, DateTime CrtDate, string UpdUser, 
    DateTime UpdDate, string VodUser,DateTime VodDate,
     
    //      from support table
    string LayananName, string DokterName)
{

    public static BookingDto FromModel(BookingModel model)
    {
        var tglBerobat = model.TglBerobat.ToDateTime(TimeOnly.MinValue);
        var jamPraktek = model.JamPraktek.ToString("HH:mm");
        var birthDate = model.Person.TglLahir.ToDateTime(TimeOnly.MinValue);
        var telpPasien = model.Person.Contact.ContactDetail;
        var result = new BookingDto(
            //      identitas
            model.BookingId, model.BookingDate,
            //      pasien
            model.Person.PersonName, birthDate,
            model.Person.Gender, model.Person.Alamat.Alamat[0],
            telpPasien, model.PasienId, model.Reg.RegId, 
            //      tujuan berobat
            tglBerobat, jamPraktek, 
            model.Layanan.LayananId, model.Dokter.PpaId, model.NoAntrian,
            //      kepesertaan bpjs
            model.NoPeserta, model.NoReffKontrol, model.ReffId,
            //      audit-trail
            model.AuditTrail.Created.UserId, model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId, model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId, model.AuditTrail.Voided.Timestamp,
            //      from support table
            model.Layanan.LayananName, model.Dokter.PpaName);
        return result;
    }

    public BookingModel ToModel()
    {
        var crt = new AuditInfoType(CrtUser, CrtDate);
        var upd = new AuditInfoType(UpdUser, UpdDate);
        var vod = new AuditInfoType(VodUser, VodDate);
        var auditTrail = new AuditTrailType(crt, upd, vod);
        var tglLahir = DateOnly.FromDateTime(TglLahir);
        var alamat = new AlamatType([Alamat], "-", "-"); 
        var tglBerobat = DateOnly.FromDateTime(TglBerobat);
        var contact = new ContactType(JenisContactEnum.Phone, TelpPasien);
        var person = new PersonInfoType(
            PasienName, tglLahir, Gender, alamat,
            contact, IdentitasType.Default);
        var layanan = new LayananReff(LayananId, LayananName);
        var dokter = new PpaReff(DokterId, DokterName);
        var jamPraktek = TimeOnly.Parse(JamPraktek);
        var reg = RegId.Trim() == string.Empty ?
            RegModel.Default.ToReff() :
            new RegReff(RegId, PasienId, PasienName);
        var result = new BookingModel(BookingId, BookingDate, person, PasienId, 
            reg, tglBerobat, jamPraktek, layanan, dokter, NoAntrian, auditTrail,
            ReffId, NoPeserta, NoReffKontrol );
        return result;
    }
}