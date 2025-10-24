using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public record Pasien2Dto(
    string PasienId,
    string AlamatKtp1,
    string AlamatKtp2,
    string AlamatKtp3,
    string AlamatKtpKota,
    string AlamatKtpKodePos) : IPasienKey
{
    public AlamatType GetAlamatKtp() 
        => new([AlamatKtp1, AlamatKtp2, AlamatKtp3], AlamatKtpKota, AlamatKtpKodePos);
    
    public static Pasien2Dto Create(PasienModel model)
    {
        var listAlamat = model.AlamatKtp.Normalize3Address();

        return new Pasien2Dto(
            model.PasienId,
            listAlamat[0],
            listAlamat[1],
            listAlamat[2],
            model.AlamatKtp.Kota,
            model.AlamatKtp.KodePos);
    }
}

public static class StringArrayExtensions
{
    public static string[] Normalize3Address(this AlamatType alamat)
    {
        var listString = alamat.Alamat;
        if (listString.Length == 0)
            listString = ["-", "-", "-"];
        if (listString.Length == 1)
            listString = listString.Concat(["-", "-"]).ToArray();
        if (listString.Length == 2)
            listString = listString.Concat(["-"]).ToArray();
            
        for(var i = 0; i < listString.Length; i++)
        {
            listString[i] = listString[i]?.Trim() ?? "-";
            listString[i] = listString[i].Length == 0 ? "-" : listString[i];
            listString[i] = listString[i].Length > 40 ? listString[i][..40] : listString[i];
        }
        return listString;        
    }
}



public class Pasien2DtoTest
{
    [Fact]
    public void UT2_Given3String_WhenCreate_ThenAsExpected()
    {
        var alamat = new string[] { "Baris-1", "Baris-2", "Baris-3" };
        var alamatType = new AlamatType(alamat, "Kota", "Kode Pos");
        var pasien = new PasienModel(
            "A", "B", DateTime.Now, "-", "nick", "X1", "X2", GolDarahType.Default,
            AlamatType.Default, alamatType, KelurahanType.Default, 
            IdentitasType.Default, IdentitasType.Default,
            [], PasienKeluargaType.Default, StatusKawinDkType.Default, AgamaType.Default, SukuType.Default,
            PekerjaanDkType.Default, PendidikanDkType.Default, DateTime.Now, true);
        var dto = Pasien2Dto.Create(pasien);

        dto.Should().NotBeNull();
        dto.AlamatKtp1.Should().Be("Baris-1");
        dto.AlamatKtp2.Should().Be("Baris-2");
        dto.AlamatKtp3.Should().Be("Baris-3");
    }

    [Fact]
    public void UT2_Given2String_WhenCreate_ThenAsExpected()
    {
        var alamat = new[] { "Baris-1", ""};
        var alamatType = new AlamatType(alamat, "Kota", "Kode Pos");
        var pasien = new PasienModel(
            "A", "B", DateTime.Now, "-", "nick", "X1", "X2", GolDarahType.Default,
            AlamatType.Default, alamatType, KelurahanType.Default, 
            IdentitasType.Default, IdentitasType.Default,
            [], PasienKeluargaType.Default, StatusKawinDkType.Default, AgamaType.Default, SukuType.Default,
            PekerjaanDkType.Default, PendidikanDkType.Default, DateTime.Now, true);
        var dto = Pasien2Dto.Create(pasien);

        dto.Should().NotBeNull();
        dto.AlamatKtp1.Should().Be("Baris-1");
        dto.AlamatKtp2.Should().Be("-");
        dto.AlamatKtp3.Should().Be("-");
    }
    
    [Fact]
    public void UT3_Given12String_WhenCreate_ThenAsExpected()
    {
        var alamat = new[] { "Baris-1"};
        var alamatType = new AlamatType(alamat, "Kota", "Kode Pos");
        var pasien = new PasienModel(
            "A", "B", DateTime.Now, "-", "nick", "X1", "X2", GolDarahType.Default,
            AlamatType.Default, alamatType, KelurahanType.Default, 
            IdentitasType.Default, IdentitasType.Default,
            [], PasienKeluargaType.Default, StatusKawinDkType.Default, AgamaType.Default, SukuType.Default,
            PekerjaanDkType.Default, PendidikanDkType.Default, DateTime.Now, true);
        var dto = Pasien2Dto.Create(pasien);

        dto.Should().NotBeNull();
        dto.AlamatKtp1.Should().Be("Baris-1");
        dto.AlamatKtp2.Should().Be("-");
        dto.AlamatKtp3.Should().Be("-");
    }

}