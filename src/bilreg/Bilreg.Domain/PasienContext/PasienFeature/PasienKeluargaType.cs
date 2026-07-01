namespace Bilreg.Domain.PasienContext.PasienFeature;

public record PasienKeluargaType(string Name, string Relasi, ContactType Contact, AlamatType Alamat)
{
    public static PasienKeluargaType Default => new PasienKeluargaType(string.Empty, string.Empty, 
        ContactType.Default, AlamatType.Default);
}