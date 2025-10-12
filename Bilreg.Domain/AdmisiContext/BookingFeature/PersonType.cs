using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public record PersonType
{
    public PersonType(string personName, DateTime birthDate, GenderType gender, 
        AlamatType alamat, ContactType contact, IdentitasType identity)
    {
        Guard.Against.NullOrWhiteSpace(personName, nameof(personName));
        Guard.Against.Null(gender, nameof(gender));
        Guard.Against.Null(birthDate, nameof(birthDate));
        Guard.Against.Null(alamat, nameof(alamat));
        Guard.Against.Null(contact, nameof(contact));
        Guard.Against.Null(identity, nameof(identity));
        
        PersonName = personName;
        BirthDate = birthDate;
        Gender = gender;
        Alamat = alamat;
        Contact = contact;
        Identity = identity;
    }
    public string PersonName { get; init; } 
    public DateTime BirthDate { get; init; } 
    public GenderType Gender { get; init; }
    public AlamatType Alamat { get; init; } 
    public ContactType Contact { get; init; }
    public IdentitasType Identity { get; init; }

    public static PersonType Default =>
        new PersonType("-", new DateTime(3000, 1, 1), GenderType.Default, AlamatType.Default, 
            ContactType.Default, IdentitasType.Default);
}