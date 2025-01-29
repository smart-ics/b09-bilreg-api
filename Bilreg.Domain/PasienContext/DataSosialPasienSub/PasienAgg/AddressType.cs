namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public record AddressType(string Alamat, string Alamat2, string Alamat3, string Kota, string KodePos)
{
    public static AddressType Default => new AddressType(string.Empty, 
        string.Empty, string.Empty, string.Empty, string.Empty);
}