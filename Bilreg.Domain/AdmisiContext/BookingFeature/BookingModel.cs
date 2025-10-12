using Bilreg.Domain.Helpers.CommonValueObjects;
using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public class BookingModel : IBookingKey
{
    private readonly List<BookingKunjunganModel> _listKunjungan;
    
    #region CREATION
    public BookingModel(string bookindId, AuditTrailType auditTrail, 
        PersonType person, DateOnly tglBerobat, IEnumerable<BookingKunjunganModel> listKunjungan)
    {
        BookingId = bookindId;
        AuditTrail = auditTrail;
        Person = person;
        TglBerobat = tglBerobat;
        _listKunjungan = listKunjungan.ToList();
    }
    
    public static BookingModel Default => new("-", AuditTrailType.Default, 
        PersonType.Default, DateOnly.FromDateTime(DateTime.Now), []);
    
    public static IBookingKey Key(string id) => new BookingModel(id, AuditTrailType.Default, 
        PersonType.Default, DateOnly.FromDateTime(DateTime.Now), []);
    
    public static BookingModel Create(PersonType person, DateOnly tglBerobat, JadwalPraktekType jadwal)
    {
        Guard.Against.Null(person, nameof(person));
        Guard.Against.Null(tglBerobat);
        Guard.Against.Null(jadwal, nameof(jadwal));
        
        if (tglBerobat.DayOfWeek != jadwal.Hari)
            throw new ArgumentException("Tanggal berobat tidak sesuai dengan jadwal");
        
        var newId = Ulid.NewUlid().ToString();
        var result = new BookingModel(newId, AuditTrailType.Create("", DateTime.Now), person, tglBerobat, []);
        result.AddKunjungan(jadwal);
        return result;
    }
    
    #endregion
    
    #region PROPERTIES
    public string BookingId { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public PersonType Person { get; init; }
    public DateOnly TglBerobat { get; init; }
    public IEnumerable<BookingKunjunganModel> ListKunjungan => _listKunjungan;
    #endregion
    
    #region BEHAVIOUR
    public void AddKunjungan(JadwalPraktekType jadwal)
    {
        var newKunjungan = BookingKunjunganModel.Create(jadwal.Layanan, jadwal.Dokter, 
            jadwal.JamMulai);
        _listKunjungan.Add(newKunjungan);
    }
    #endregion
}