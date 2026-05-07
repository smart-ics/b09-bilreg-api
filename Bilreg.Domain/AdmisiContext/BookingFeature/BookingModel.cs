using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public class BookingModel : IBookingKey
{
    #region CREATION

    public BookingModel(string bookindId, DateTime bookingDate,
        PersonInfoType person, string pasienId, RegReff reg, DateOnly tglBerobat, TimeOnly jamPraktek,
        LayananReff layanan, PpaReff dokter, int noAntrian,
        AuditTrailType auditTrail, ExtAppReffType extBookingReff, CoverageInfoType coverageInfo)
    {
        BookingId = bookindId;
        BookingDate = bookingDate;

        Person = person;
        PasienId = pasienId;
        Reg = reg;

        TglBerobat = tglBerobat;
        JamPraktek = jamPraktek;
        Layanan = layanan;
        Dokter = dokter;
        NoAntrian = noAntrian;

        AuditTrail = auditTrail;

        ExtAppReff = extBookingReff;
        CoverageInfo = coverageInfo;
    }

    public static BookingModel Default => new("-", new DateTime(3000,1,1),
        PersonInfoType.Default, "-", RegModel.Default.ToReff(), DateOnly.MinValue, TimeOnly.MinValue, 
        LayananType.Default.ToReff(), PpaType.Default.ToReff(), 0, 
        AuditTrailType.Default, ExtAppReffType.Default, CoverageInfoType.Default);
    
    public static IBookingKey Key(string id) => new BookingModel(id, new DateTime(3000,1,1), 
        PersonInfoType.Default, "-", RegModel.Default.ToReff(), DateOnly.MinValue, TimeOnly.MinValue, 
        LayananType.Default.ToReff(), PpaType.Default.ToReff(), 0, 
        AuditTrailType.Default, ExtAppReffType.Default, CoverageInfoType.Default);
    
    public static BookingModel CreateLocal(PersonInfoType person, DateOnly tglBerobat, 
        JadwalPraktekType jadwal, string userId)
    {
        Guard.Against.Null(person);
        Guard.Against.Null(tglBerobat);
        Guard.Against.Null(jadwal);
        
        if (tglBerobat.DayOfWeek != jadwal.Hari)
            throw new ArgumentException("Tanggal berobat tidak sesuai dengan jadwal");
        
        var newId = Ulid.NewUlid().ToString();
        var result = new BookingModel(newId, DateTime.Now, person, "-", RegModel.Default.ToReff(), 
            tglBerobat, jadwal.JamMulai, jadwal.Layanan, jadwal.Dokter,  -1, 
            AuditTrailType.Create(userId, DateTime.Now), 
            ExtAppReffType.Default, CoverageInfoType.Default);
        return result;
    }
    public static BookingModel CreateFromExternal(PersonInfoType person, DateOnly tglBerobat, 
        JadwalPraktekType jadwal, ExtAppReffType extAppReff, CoverageInfoType coverage, string userId)
    {
        Guard.Against.Null(person);
        Guard.Against.Null(tglBerobat);
        Guard.Against.Null(jadwal);
        Guard.Against.Null(extAppReff);
        
        if (tglBerobat.DayOfWeek != jadwal.Hari)
            throw new ArgumentException("Tanggal berobat tidak sesuai dengan jadwal");
        
        if (extAppReff.ExtAppName.Length == 0)
            throw new ArgumentException("Source External Booking tidak boleh kosong");
        
        var newId = Ulid.NewUlid().ToString();
        var result = new BookingModel(newId, DateTime.Now, person, "-", RegModel.Default.ToReff(), 
            tglBerobat, jadwal.JamMulai, jadwal.Layanan, jadwal.Dokter,  -1, 
            AuditTrailType.Create(userId, DateTime.Now), 
            extAppReff, coverage);
        return result;
    }
    

    #endregion

    #region PROPERTIES
    public string BookingId { get; init; }
    public DateTime BookingDate { get; init; }

    public PersonInfoType Person { get; init; }
    public string PasienId { get; private set; }
    public RegReff Reg { get; private set; }

    public DateOnly TglBerobat { get; init; }
    public TimeOnly JamPraktek { get; init; }
    public LayananReff Layanan { get; init; }
    public PpaReff Dokter { get; init; }
    public int NoAntrian { get; private set; }
    public ExtAppReffType ExtAppReff { get; private set; }
    public CoverageInfoType CoverageInfo { get; private set; }
    public AuditTrailType AuditTrail { get; init; }
    #endregion

    #region BEHAVIOUR
    public void AssignNoAntrian(int noAntrian)
    {
        NoAntrian = noAntrian;
    }

    public void ResolvePasienId(PasienModel pasien)
    {
        var hasTglLahir = Person.TglLahir != new DateOnly(3000, 1, 1);

        var isValid = hasTglLahir
            ? Person.IsSimilar(pasien.Person)
            : Person.IsSimilarName(pasien.Person);

        if (!isValid)
        {
            var rsInfo = hasTglLahir
                ? $"Tgl Lahir = {pasien.Person.TglLahir:yyyy-MM-dd} - Nama = {pasien.Person.PersonName}"
                : $"Nama = {pasien.Person.PersonName}";

            var bookingInfo = hasTglLahir
                ? $"Tgl Lahir = {Person.TglLahir:yyyy-MM-dd} - Nama = {Person.PersonName}"
                : $"Nama = {Person.PersonName}";

            throw new ArgumentException(
                $"Pasien terpilih tidak sesuai dengan booking. " +
                $"Data RS : {rsInfo}. " +
                $"Data Booking : {bookingInfo}");
        }

        PasienId = pasien.PasienId;
    }
    public void AssignReg(RegModel reg)
    {
        if (reg.Pasien.PasienId != PasienId)
            throw new ArgumentException("Kode MR di Registrasi tidak sesuai dengan booking");
        Reg = reg.ToReff();
    }

    public void UnRegister()
    {
        Reg = new RegReff("-", PasienId, Person.PersonName);
    }

    public void AttachCoverage(CoverageInfoType coverage)
    {
        CoverageInfo = coverage;
    }

    public void SetExtApp(ExtAppReffType extApp)
    {
        ExtAppReff = extApp; 
    }
    public bool HasBeenRegistered() 
        => Reg.RegId is not ("" or "-");
    #endregion
}

public record ExtAppReffType(
    string ExtAppName,
    string ReffId,
    string CheckInQr)
{
    public static ExtAppReffType Default => new("", "", "");
}
public record CoverageInfoType(
    string AsuransiName, 
    string NoPeserta,
    string NoRujukan)
{
    public static CoverageInfoType Default => new("", "", "");
};


public record BookingView(
    string BookingId, DateTime BookingDate, PersonInfoType Person,
    RegReff Reg, DateOnly TglBerobat, TimeOnly JamPraktek,
    LayananReff Layanan, PpaReff Dokter, int NoAntrian);

public record BookingExtView(string BookingId, DateTime BookingDate,
    DateTime TglBerobat, PersonInfoType Person, RegReff Reg, 
    LayananReff Layanan, PpaReff Dokter, TimeOnly JamPraktek, int NoAntrian,
    ExtAppReffType ExtAppReff, CoverageInfoType CoverageInfo) : IBookingKey;