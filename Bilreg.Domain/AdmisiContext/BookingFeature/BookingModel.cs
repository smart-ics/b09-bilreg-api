using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
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
        LayananReff layanan, PpaReff dokter,  int noAntrian,
        AuditTrailType auditTrail)
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
    }

    public static BookingModel Default => new("-", new DateTime(3000,1,1),
        PersonInfoType.Default, "-", RegModel.Default.ToReff(), DateOnly.MinValue, TimeOnly.MinValue, 
        LayananType.Default.ToReff(), PpaType.Default.ToReff(), 0, 
        AuditTrailType.Default);
    
    public static IBookingKey Key(string id) => new BookingModel(id, new DateTime(3000,1,1), 
        PersonInfoType.Default, "-", RegModel.Default.ToReff(), DateOnly.MinValue, TimeOnly.MinValue, 
        LayananType.Default.ToReff(), PpaType.Default.ToReff(), 0, 
        AuditTrailType.Default);
    
    public static BookingModel Create(PersonInfoType person, DateOnly tglBerobat, JadwalPraktekType jadwal)
    {
        Guard.Against.Null(person, nameof(person));
        Guard.Against.Null(tglBerobat);
        Guard.Against.Null(jadwal, nameof(jadwal));
        
        if (tglBerobat.DayOfWeek != jadwal.Hari)
            throw new ArgumentException("Tanggal berobat tidak sesuai dengan jadwal");
        
        var newId = Ulid.NewUlid().ToString();
        var result = new BookingModel(newId, DateTime.Now, person, "-", RegModel.Default.ToReff(), 
            tglBerobat, jadwal.JamMulai, jadwal.Layanan, jadwal.Dokter,  -1, 
            AuditTrailType.Create("", DateTime.Now));
        return result;
    }
    #endregion
    
    #region PROPERTIES
    public string BookingId { get; init; }
    public DateTime BookingDate { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public PersonInfoType Person { get; init; }
    public string PasienId { get; private set; }
    public RegReff Reg { get; private set; }
    public DateOnly TglBerobat { get; init; }
    public TimeOnly JamPraktek { get; init; }
    public LayananReff Layanan { get; init; }
    public PpaReff Dokter { get; init; }
    public int NoAntrian { get; private set; }
    #endregion

    #region BEHAVIOUR
    public void AssignNoAntrian(int noAntrian)
    {
        NoAntrian = noAntrian;
    }

    public void ResolvePasienId(PasienModel pasien)
    {
        if (!Person.IsSimilar(pasien.Person))
            throw new ArgumentException("Pasien tidak sesuai dengan booking");

        PasienId = pasien.PasienId;
    }
    public void AssignReg(RegModel reg)
    {
        if (reg.Pasien.PasienId != PasienId)
            throw new ArgumentException("Kode MR di Registrasi tidak sesuai dengan booking");
        Reg = reg.ToReff();
    }
    public bool HasBeenRegistered() 
        => Reg.RegId is not ("" or "-");
    #endregion
}