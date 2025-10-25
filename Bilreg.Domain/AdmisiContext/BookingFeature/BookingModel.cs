using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Domain.Helpers.CommonValueObjects;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public class BookingModel : IBookingKey
{
    #region CREATION
    public BookingModel(string bookindId, DateTime bookingDate, 
        PersonInfoType person, DateOnly tglBerobat, TimeOnly jamPraktek,
        LayananReff layanan, PetugasMedisReff dokter,  int noAntrian,
        AuditTrailType auditTrail)
    {
        BookingId = bookindId;
        BookingDate = bookingDate;
        Person = person;
        TglBerobat = tglBerobat;
        JamPraktek = jamPraktek;
        Layanan = layanan;
        Dokter = dokter;
        NoAntrian = noAntrian;
        AuditTrail = auditTrail;
    }

    public static BookingModel Default => new("-", new DateTime(3000,1,1),
        PersonInfoType.Default, DateOnly.MinValue, TimeOnly.MinValue, 
        LayananType.Default.ToReff(), PetugasMedisType.Default.ToReff(), 0, 
        AuditTrailType.Default);
    
    public static IBookingKey Key(string id) => new BookingModel(id, new DateTime(3000,1,1), 
        PersonInfoType.Default, DateOnly.MinValue, TimeOnly.MinValue, 
        LayananType.Default.ToReff(), PetugasMedisType.Default.ToReff(), 0, 
        AuditTrailType.Default);
    
    public static BookingModel Create(PersonInfoType person, DateOnly tglBerobat, JadwalPraktekType jadwal)
    {
        Guard.Against.Null(person, nameof(person));
        Guard.Against.Null(tglBerobat);
        Guard.Against.Null(jadwal, nameof(jadwal));
        
        if (tglBerobat.DayOfWeek != jadwal.Hari)
            throw new ArgumentException("Tanggal berobat tidak sesuai dengan jadwal");
        
        var newId = Ulid.NewUlid().ToString();
        var result = new BookingModel(newId, DateTime.Now, person, 
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
    public DateOnly TglBerobat { get; init; }
    public TimeOnly JamPraktek { get; init; }
    public LayananReff Layanan { get; init; }
    public PetugasMedisReff Dokter { get; init; }
    public int NoAntrian { get; private set; }
    #endregion

    #region BEHAVIOUR
    public void AssignNoAntrian(int noAntrian)
    {
        NoAntrian = noAntrian;
    }
    #endregion
}