using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.PasienFeature;

public record ContactType
{
    public ContactType(JenisContactEnum jenisContact, string contactNo)
    {
        Guard.Against.EnumOutOfRange(jenisContact, nameof(jenisContact));
        JenisContact = jenisContact;
        ContactDetail = contactNo;
    }
    public JenisContactEnum JenisContact { get; init; }
    public string ContactDetail { get; init; }
    public static ContactType Default => new ContactType(JenisContactEnum.Other, "-");
}

public enum JenisContactEnum
{
    Other,
    Phone,
    Email,
    Fax,
}
