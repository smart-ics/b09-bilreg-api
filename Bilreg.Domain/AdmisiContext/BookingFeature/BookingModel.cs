using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Domain.Helpers.CommonValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;
using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public class BookingModel
{
    public BookingModel(string bookindId, AuditTrailType auditTrail, PersonType person, DateOnly tglBerobat)
    {
        BookindId = bookindId;
        AuditTrail = auditTrail;
        Person = person;
        TglBerobat = tglBerobat;
        ListKunjungan = [];
    }

    public string BookindId { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public PersonType Person { get; init; }
    
    public DateOnly TglBerobat { get; init; }
    public List<BookingKunjunganType> ListKunjungan { get; private set; }
}

public record BookingKunjunganType
{
    public BookingKunjunganType(LayananReff layanan, PetugasMedisReff dokter, 
        TimeOnly jamPraktek, int nomorAntrian)
    {
        Guard.Against.Null(layanan, nameof(layanan));
        Guard.Against.Null(dokter, nameof(dokter));

        Layanan = layanan;
        Dokter = dokter;
        JamPraktek = jamPraktek;
        NomorAntrian = nomorAntrian;
    }
    public LayananReff Layanan { get; init; } 
    public PetugasMedisReff Dokter { get; init; }
    public TimeOnly JamPraktek { get; init; }
    public int NomorAntrian { get; init; }
}

public record PersonType
{
    public PersonType(string personName, DateTime birthDate, 
        AlamatType alamat, ContactType contact, IdentitasType identity)
    {
        Guard.Against.NullOrWhiteSpace(personName, nameof(personName));
        Guard.Against.Null(birthDate, nameof(birthDate));
        Guard.Against.Null(alamat, nameof(alamat));
        Guard.Against.Null(contact, nameof(contact));
        Guard.Against.Null(identity, nameof(identity));
        
        PersonName = personName;
        BirthDate = birthDate;
        Alamat = alamat;
        Contact = contact;
        Identity = identity;
    }
    public string PersonName { get; init; } 
    public DateTime BirthDate { get; init; } 
    public AlamatType Alamat { get; init; } 
    public ContactType Contact { get; init; }
    public IdentitasType Identity { get; init; }

    public static PersonType Default =>
        new PersonType("-", new DateTime(3000, 1, 1), AlamatType.Default, 
            ContactType.Default, IdentitasType.Default);
}