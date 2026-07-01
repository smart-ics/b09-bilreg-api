using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public record PersonInfoType : PersonType
{
    public PersonInfoType(string personName, DateOnly tglLahir, string gender, 
        AlamatType alamat, ContactType contact, IdentitasType identity) 
        : base(personName, tglLahir)
    {
        Guard.Against.NullOrWhiteSpace(personName, nameof(personName));
        Guard.Against.Null(gender, nameof(gender));
        Guard.Against.Null(tglLahir, nameof(tglLahir));
        Guard.Against.Null(alamat, nameof(alamat));
        Guard.Against.Null(contact, nameof(contact));
        Guard.Against.Null(identity, nameof(identity));
        
        PersonName = personName;
        TglLahir = tglLahir;
        Gender = gender;
        Alamat = alamat;
        Contact = contact;
        Identity = identity;
    }

    public new static PersonInfoType Default =>
        new PersonInfoType("-", new DateOnly(3000, 1, 1), "-", AlamatType.Default,
            ContactType.Default, IdentitasType.Default);

    public string Gender { get; init; }
    public AlamatType Alamat { get; init; } 
    public ContactType Contact { get; init; }
    public IdentitasType Identity { get; private set; }


    public void SetIdentity(IdentitasType identity)
    => Identity = identity;
}