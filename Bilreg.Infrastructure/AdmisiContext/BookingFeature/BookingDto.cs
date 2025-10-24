using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Domain.Helpers.CommonValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public record BookingDto(
    //      main table
    string BookingId, DateTime BookingDate,     
    string PasienName, DateTime TglLahir, string Gender, string Alamat,
    DateTime TglBerobat, string JamPraktek, 
    string LayananId, string DokterId, int NoAntrian,
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
        var result = new BookingDto(
            //      identitas
            model.BookingId, model.BookingDate,
            //      pasien
            model.Person.PersonName, birthDate,
            model.Person.Gender, model.Person.Alamat.Alamat[0],
            //      tujuan berobat
            tglBerobat, jamPraktek, 
            model.Layanan.LayananId, model.Dokter.PetugasMedisId, model.NoAntrian,
            //      audit-trail
            model.AuditTrail.Created.UserId, model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId, model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId, model.AuditTrail.Voided.Timestamp,
            //      from support table
            model.Layanan.LayananName, model.Dokter.PetugasMedisName);
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
        
        var person = new PersonInfoType(
            PasienName, tglLahir, Gender, alamat,
            ContactType.Default, IdentitasType.Default);
        var layanan = new LayananReff(LayananId, LayananName);
        var dokter = new PetugasMedisReff(DokterId, DokterName);
        var jamPraktek = TimeOnly.Parse(JamPraktek);
        var result = new BookingModel(BookingId, BookingDate, person,
            tglBerobat, jamPraktek, layanan, dokter, NoAntrian, auditTrail);
        return result;
    }
}