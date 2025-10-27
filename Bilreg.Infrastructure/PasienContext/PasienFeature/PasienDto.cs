using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.ValidationHelper;
// ReSharper disable InconsistentNaming

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public record PasienDto(
    string fs_mr, string fs_nm_pasien, string fd_tgl_lahir, string fs_jns_kelamin,
    string fs_nm_alias, string fs_temp_lahir, string fs_nm_ibu_kandung, string fs_gol_darah,
    string fs_alm_pasien, string fs_alm2_pasien, string fs_alm3_pasien, string fs_kota_pasien,
    string fs_kd_pos_pasien, string fs_kd_kelurahan,  
    //
    string fs_jenis_id, string fs_kd_identitas, string fs_no_kk, 
    string fs_email, string fs_tlp_pasien, string fs_no_hp,
    //
    string fs_nm_keluarga, string fs_hub_keluarga, string fs_telp_keluarga,
    string fs_alm1_keluarga, string fs_alm2_keluarga, 
    string fs_kota_keluarga, string fs_kd_pos_keluarga, 
    //
    string fs_kd_agama, string fs_kd_suku, string fs_kd_status_kawin_dk, 
    string fs_kd_pendidikan_dk, string fs_kd_pekerjaan_dk, 
    //
    string fd_tgl_mr, bool fb_aktif,
    //
    string fs_nm_kelurahan, string fs_kd_kecamatan, string fs_nm_kecamatan, 
    string fs_kd_kabupaten, string fs_nm_kabupaten, 
    string fs_kd_propinsi, string fs_nm_propinsi,
    string fs_nm_agama, string fs_nm_suku, string fs_nm_status_kawin_dk, 
    string fs_nm_pendidikan_dk, string fs_nm_pekerjaan_dk)
{
    public static PasienDto FromModel(PasienModel model)
    {
        var listAlamat = model.Person.Alamat.Normalize3Address();
        var kel = model.Kelurahan;
        var email = model.ListContact
            .FirstOrDefault(x => x.JenisContact == JenisContactEnum.Email)
            ?.ContactDetail ?? "-";
        var noTelp = model.ListContact
            .FirstOrDefault(x => x.JenisContact == JenisContactEnum.Phone)
            ?.ContactDetail ?? "-";
        var noHp = model.ListContact
            .FirstOrDefault(x => x.JenisContact == JenisContactEnum.Mobile)
            ?.ContactDetail ?? "-";
        var klg = model.PasienKeluarga;
        var identitas = model.Person.Identity;

        var result = new PasienDto(
            model.PasienId, model.Person.PersonName, model.Person.TglLahir.ToString("yyyy-MM-dd"),
            model.Person.Gender, model.NickName, model.TempatLahir, model.NamaIbuKandung, model.GolDarah.ToString(),
            listAlamat[0], listAlamat[1], listAlamat[2], model.Person.Alamat.Kota, 
            model.Person.Alamat.KodePos, kel.KelurahanId, 
            //
            identitas.JenisId, identitas.NomorId, model.KartuKeluarga.NomorId,
            email, noTelp, noHp,
            //
            klg.Name, klg.Relasi, klg.Contact.ContactDetail,
            klg.Alamat.Normalize3Address()[0], klg.Alamat.Normalize3Address()[1],
            klg.Alamat.Kota, klg.Alamat.KodePos,
            //
            model.Agama.AgamaId, model.Suku.SukuId, model.StatusKawin.StatusKawinDkId,
            model.PendidikanDk.PendidikanDkId, model.PekerjaanDk.PekerjaanDkId,
            //
            model.TglMedRec.ToString("yyyy-MM-dd"), model.IsAktif,
            //
            kel.KelurahanName, kel.Kecamatan.KecamatanId, kel.Kecamatan.KecamatanName,
            kel.Kabupaten.KabupatenId, kel.Kabupaten.KabupatenName, 
            kel.Propinsi.PropinsiId, kel.Propinsi.PropinsiName,
            //
            model.Agama.AgamaName, model.Suku.SukuName, model.StatusKawin.StatusKawinDkName,
            model.PendidikanDk.PendidikanDkName, model.PekerjaanDk.PekerjaanDkName);
        return result;
    }
    //
    //
    // public PasienModel ToModel()
    // {
    //     //      personal info
    //     var tglLahir = fd_tgl_lahir.ToDate(DateFormatEnum.YMD);
    //     var gender = fs_jns_kelamin;
    //     var golDarah = new GolDarahType(fs_gol_darah);
    //     var alamat = new[] {fs_alm_pasien, fs_alm2_pasien, fs_alm3_pasien};
    //     var alamatDomisili = new AlamatType(alamat, fs_kota_pasien, fs_kd_pos_pasien);
    //     var cpMobile = new ContactType(JenisContactEnum.Mobile, fs_no_hp);
    //     var cpEmail = new ContactType(JenisContactEnum.Email, fs_email);
    //     var cpTelp = new ContactType(JenisContactEnum.Phone, fs_tlp_pasien);
    //     var listContact = new[] {cpEmail, cpTelp, cpMobile};
    //     var identitas = new IdentitasType(fs_jenis_id, fs_kd_identitas);
    //     var person = new PersonInfoType(fs_nm_pasien, DateOnly.Parse(fd_tgl_lahir), fs_jns_kelamin,
    //         alamatDomisili, cpMobile, identitas);
    //     
    //     //      administrative  
    //     var alamatKtp = new AlamatType(alamat, fs_kota_pasien, fs_kd_pos_pasien);
    //     var propinsi = new PropinsiType(fs_kd_propinsi, fs_nm_propinsi);
    //     var kabupaten = new KabupatenType(fs_kd_kabupaten, fs_nm_kabupaten, propinsi);
    //     var kecamatan = new KecamatanType(fs_kd_kecamatan, fs_nm_kecamatan, kabupaten.ToReff(), propinsi);
    //     var kelurahan = new KelurahanType(fs_kd_kelurahan, fs_nm_kelurahan, kecamatan.ToReff(),
    //         kabupaten.ToReff(), propinsi);
    //     var identitasKk = new IdentitasType("KK", fs_no_kk);
    //
    //     var contactKeluarga = new ContactType(JenisContactEnum.Mobile, fs_telp_keluarga);
    //     var alamatKlg = new []{fs_alm1_keluarga, fs_alm2_keluarga, string.Empty};
    //     var addressKeluarga = new AlamatType(alamatKlg, fs_kota_pasien, fs_kd_pos_keluarga);
    //     var keluarga = new PasienKeluargaType(fs_nm_keluarga, fs_hub_keluarga, 
    //         contactKeluarga, addressKeluarga);
    //     
    //     //      status sosial
    //     var statusKawin = new StatusKawinDkType(fs_kd_status_kawin_dk, fs_nm_status_kawin_dk);
    //     var agama = new AgamaType(fs_kd_agama, fs_nm_agama);
    //     var suku = new SukuType(fs_kd_suku, fs_nm_suku);
    //     var pekerjaan = new PekerjaanDkType(fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk);
    //     var pendidikan = new PendidikanDkType(fs_kd_pendidikan_dk, fs_nm_pendidikan_dk);
    //
    //     //      olah berkas
    //     var tglMedRec = fd_tgl_mr.ToDate(DateFormatEnum.YMD);
    //
    //     var pasien = new PasienModel(fs_mr, person,
    //         fs_nm_alias, fs_temp_lahir, golDarah, fs_nm_ibu_kandung,
    //         kelurahan, 
    //         alamatDomisili, AlamatType.Default, kelurahan, identitas, identitasKk,
    //         listContact, keluarga, statusKawin, agama, suku, pekerjaan, pendidikan,
    //         tglMedRec, fb_aktif);
    //
    //     return pasien;
    // }
}

public static class DtoExtensions
{
    public static void ToDefaultString<T>(this T obj)
    {
        if (EqualityComparer<T>.Default.Equals(obj, default))
        {
            return;
        }
    
        var properties = from p in typeof(T).GetProperties()
            where p.PropertyType == typeof(string) &&
                  p.CanRead &&
                  p.CanWrite
            select p;

        foreach (var property in properties)
        {
            var value = (string)property.GetValue(obj, null);
            if (!string.IsNullOrEmpty(value)) 
                continue;
            var replacement = property.Name.StartsWith("fd", StringComparison.OrdinalIgnoreCase) 
                ? "3000-01-01" 
                : "-";
                
            property.SetValue(obj, replacement, null);
        }
    }
}