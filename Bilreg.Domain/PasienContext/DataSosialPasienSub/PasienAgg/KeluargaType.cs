namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public record KeluargaType(string Name, string Relasi, ContactType Contact, AddressType Address)
{
    public static KeluargaType Default => new KeluargaType(string.Empty, string.Empty, 
        ContactType.Default, AddressType.Default);
}