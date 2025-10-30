using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;


// ReSharper disable InconsistentNaming
public record PasienKtpDto(
    string fs_kd_mr, string fs_nik, string fs_nama_ktp, string fs_alm_ktp,
    string fs_rt_ktp, string fs_rw_ktp, string fs_kd_kelurahan_ktp, string fs_kelurahan_ktp, 
    string fs_kd_kecamatan_ktp, string fs_kecamatan_ktp, string fs_kd_kabupaten_ktp, string fs_kabupaten_ktp,
    string fs_kd_propinsi_ktp, string fs_propinsi_ktp,
    string fs_tempat_lahir, string fs_sex, string fd_tgl_lahir, string fs_gol_darah)
{
    public static PasienKtpDto FromModel(PasienModel model)
    {
        var ktp = model.Ktp;
        var alamat = string.Join(",", ktp.Alamat);
        var result = new PasienKtpDto(
            model.PasienId, ktp.Nik, model.Person.PersonName, alamat,
            ktp.Rt, ktp.Rw, ktp.Kelurahan.KelurahanId, ktp.Kelurahan.KelurahanName,
            ktp.Kelurahan.Kecamatan.KecamatanId, ktp.Kelurahan.Kecamatan.KecamatanName,
            ktp.Kelurahan.Kabupaten.KabupatenId, ktp.Kelurahan.Kabupaten.KabupatenName,
            ktp.Kelurahan.Propinsi.PropinsiId, ktp.Kelurahan.Propinsi.PropinsiName,
            model.TempatLahir, model.Person.TglLahir.ToString("yyyy-MM-dd"), model.Person.Gender,
            model.GolDarah.ToString());
        return result;
    }
    public static PasienKtpDto Default 
        => new PasienKtpDto("-", "-", "-", "-", "-", "-",
        "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-","-");
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
//
//
//
// public class PasienKtpDtoTest
// {
//     [Fact]
//     public void UT2_Given3String_WhenCreate_ThenAsExpected()
//     {
//         var alamat = new string[] { "Baris-1", "Baris-2", "Baris-3" };
//         var alamatType = new AlamatType(alamat, "Kota", "Kode Pos");
//         var person = new PersonInfoType("B", DateOnly.FromDateTime(DateTime.Now), "P",
//             AlamatType.Default, ContactType.Default, IdentitasType.Default);
//         var pasien = new PasienModel("A", person, "B", "C", GolDarahType.AB,
//             "ibu kandung", alamatType,KelurahanType.Default, 
//             IdentitasType.Default, [], PasienKeluargaType.Default, 
//             AgamaType.Default, SukuType.Default, StatusKawinDkType.Default,
//             PendidikanDkType.Default, PekerjaanDkType.Default, DateTime.Now, true);
//         var dto = PasienKtpDto.Create(pasien);
//
//         dto.Should().NotBeNull();
//         dto.AlamatKtp1.Should().Be("Baris-1");
//         dto.AlamatKtp2.Should().Be("Baris-2");
//         dto.AlamatKtp3.Should().Be("Baris-3");
//     }
//
//     [Fact]
//     public void UT2_Given2String_WhenCreate_ThenAsExpected()
//     {
//         var alamat = new[] { "Baris-1", ""};
//         var alamatType = new AlamatType(alamat, "Kota", "Kode Pos");
//         var person = new PersonInfoType("B", DateOnly.FromDateTime(DateTime.Now), "P",
//             AlamatType.Default, ContactType.Default, IdentitasType.Default);
//         var pasien = new PasienModel("A", person, "B", "C", GolDarahType.AB,
//             "ibu kandung", alamatType,KelurahanType.Default, 
//             IdentitasType.Default, [], PasienKeluargaType.Default, 
//             AgamaType.Default, SukuType.Default, StatusKawinDkType.Default,
//             PendidikanDkType.Default, PekerjaanDkType.Default, DateTime.Now, true);
//         var dto = PasienKtpDto.Create(pasien);
//
//         dto.Should().NotBeNull();
//         dto.AlamatKtp1.Should().Be("Baris-1");
//         dto.AlamatKtp2.Should().Be("-");
//         dto.AlamatKtp3.Should().Be("-");
//     }
//     
//     [Fact]
//     public void UT3_Given12String_WhenCreate_ThenAsExpected()
//     {
//         var alamat = new[] { "Baris-1"};
//         var alamatType = new AlamatType(alamat, "Kota", "Kode Pos");
//         var person = new PersonInfoType("B", DateOnly.FromDateTime(DateTime.Now), "P",
//             AlamatType.Default, ContactType.Default, IdentitasType.Default);
//         var pasien = new PasienModel("A", person, "B", "C", GolDarahType.AB,
//             "ibu kandung", alamatType,KelurahanType.Default, 
//             IdentitasType.Default, [], PasienKeluargaType.Default, 
//             AgamaType.Default, SukuType.Default, StatusKawinDkType.Default,
//             PendidikanDkType.Default, PekerjaanDkType.Default, DateTime.Now, true);
//         var dto = PasienKtpDto.Create(pasien);
//
//         dto.Should().NotBeNull();
//         dto.AlamatKtp1.Should().Be("Baris-1");
//         dto.AlamatKtp2.Should().Be("-");
//         dto.AlamatKtp3.Should().Be("-");
//     }
//
// }