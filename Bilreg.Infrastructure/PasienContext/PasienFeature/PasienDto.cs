using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public class PasienDto 
{
    public PasienDto()
    {
    }

    public PasienDto(PasienModel model)
    {
        fs_mr = model.PasienId;
        fs_nm_pasien = model.PasienName;
        fd_tgl_lahir = model.TglLahir.ToString("yyyy-MM-dd");
        fs_jns_kelamin = model.Gender.Symbol;

        fs_nm_alias = model.NickName;
        fs_temp_lahir = model.TempatLahir;
        fs_nm_ibu_kandung = model.IbuKandung;
        fs_gol_darah = model.GolDarah.ToString();
        fs_alm_pasien = model.AlamatDomisili.Alamat[0];
        fs_alm2_pasien = model.AlamatDomisili.Alamat[1];
        fs_alm3_pasien = model.AlamatDomisili.Alamat[2];
        fs_kota_pasien = model.AlamatDomisili.Kota;
        fs_kd_pos_pasien = model.AlamatDomisili.KodePos;
        fs_kd_kelurahan = model.Kelurahan.KelurahanId;
        fs_jenis_id = model.Identitas.JenisId;
        fs_kd_identitas = model.Identitas.NomorId;
        fs_no_kk = model.KartuKeluarga.NomorId;
        
        fs_email = model.ListContact
            .FirstOrDefault(x => x.JenisContact == JenisContactEnum.Email)
            ?.ContactDetail ?? "-";
        fs_tlp_pasien = model.ListContact
            .FirstOrDefault(x => x.JenisContact == JenisContactEnum.Phone)
            ?.ContactDetail ?? "-";
        fs_no_hp = model.ListContact
            .FirstOrDefault(x => x.JenisContact == JenisContactEnum.Mobile)
            ?.ContactDetail ?? "-";
        
        fs_nm_keluarga = model.PasienKeluarga.Name;
        fs_hub_keluarga = model.PasienKeluarga.Relasi;
        fs_telp_keluarga = model.PasienKeluarga.Contact.ContactDetail;
        fs_alm1_keluarga = model.PasienKeluarga.Alamat.Alamat[0];
        fs_alm2_keluarga = model.PasienKeluarga.Alamat.Alamat[1];
        fs_kota_keluarga = model.PasienKeluarga.Alamat.Kota;
        fs_kd_pos_keluarga = model.PasienKeluarga.Alamat.KodePos;
        
        fs_kd_status_kawin_dk = model.StatusKawin.StatusKawinDkId;
        fs_kd_agama = model.Agama.AgamaId;
        fs_kd_suku = model.Suku.SukuId;
        fs_kd_pekerjaan_dk = model.PekerjaanDk.PekerjaanDkId;
        fs_kd_pendidikan_dk = model.PendidikanDk.PendidikanDkId;
        
        fd_tgl_mr = model.TglMedRec.ToString("yyyy-MM-dd");
        fb_aktif = model.IsAktif;
    }
    
    #region PROPERTIES
    public string fs_mr { get; set; }
    public string fs_nm_pasien { get; set; }
    public string fd_tgl_lahir { get; set; }
    public string fs_jns_kelamin {get; set;}

    public string fs_nm_alias { get; set; }
    public string fs_temp_lahir { get; set; }
    public string fs_nm_ibu_kandung {get; set;}
    public string fs_gol_darah {get; set;}


    public string fs_alm_pasien {get; set;}
    public string fs_alm2_pasien {get; set;}
    public string fs_alm3_pasien {get; set;}
    public string fs_kota_pasien {get; set;}
    public string fs_kd_pos_pasien {get; set;}
    public string fs_kd_kelurahan {get; set;}
    public string fs_nm_kelurahan {get; set;}
    public string fs_kd_kecamatan { get; set; }
    public string fs_nm_kecamatan {get; set;}
    public string fs_kd_kabupaten { get; set; }
    public string fs_nm_kabupaten {get; set;}
    public string fs_kd_propinsi { get; set; }
    public string fs_nm_propinsi {get; set;}
    

    public string fs_jenis_id {get; set;}
    public string fs_kd_identitas {get; set;}
    public string fs_no_kk {get; set;}
    public string fs_email {get; set;}
    public string fs_tlp_pasien {get; set;}
    public string fs_no_hp {get; set;}

    
    public string fs_nm_keluarga {get; set;}
    public string fs_hub_keluarga {get; set;}
    public string fs_telp_keluarga {get; set;}
    public string fs_alm1_keluarga {get; set;}
    public string fs_alm2_keluarga {get; set;}
    public string fs_kota_keluarga {get; set;}
    public string fs_kd_pos_keluarga {get; set;}


    public string fs_kd_status_kawin_dk {get; set;}
    public string fs_nm_status_kawin_dk {get; set;}
    public string fs_kd_agama {get; set;}
    public string fs_nm_agama {get; set;}
    public string fs_kd_suku {get; set;}
    public string fs_nm_suku {get; set;}
    public string fs_kd_pekerjaan_dk {get; set;}
    public string fs_nm_pekerjaan_dk {get; set;}
    public string fs_kd_pendidikan_dk {get; set;}
    public string fs_nm_pendidikan_dk {get; set;}
    
    public string fd_tgl_mr {get; set;}
    public bool fb_aktif { get; set; }
    #endregion

    public PasienModel ToModel(IGenderDal genderDal)
    {
        //      personal info
        var tglLahir = fd_tgl_lahir.ToDate(DateFormatEnum.YMD);
        var gender = genderDal.GetData(fs_jns_kelamin).Value;
        var golDarah = new GolDarahType(fs_gol_darah);
        
        //      administrative  
        var alamat = new[] {fs_alm_pasien, fs_alm2_pasien, fs_alm3_pasien};
        var alamatDomisili = new AlamatType(alamat, fs_kota_pasien, fs_kd_pos_pasien);
        var propinsi = new PropinsiType(fs_kd_propinsi, fs_nm_propinsi);
        var kabupaten = new KabupatenType(fs_kd_kabupaten, fs_nm_kabupaten, propinsi);
        var kecamatan = new KecamatanType(fs_kd_kecamatan, fs_nm_kecamatan, kabupaten.ToReff(), propinsi);
        var kelurahan = new KelurahanType(fs_kd_kelurahan, fs_nm_kelurahan, kecamatan.ToReff(),
            kabupaten.ToReff(), propinsi);
        var identitas = new IdentitasType(fs_jenis_id, fs_kd_identitas);
        var identitasKk = new IdentitasType("KK", fs_no_kk);

        var cpEmail = new ContactType(JenisContactEnum.Email, fs_email);
        var cpTelp = new ContactType(JenisContactEnum.Phone, fs_tlp_pasien);
        var cpMobile = new ContactType(JenisContactEnum.Mobile, fs_no_hp);
        var listContact = new[] {cpEmail, cpTelp, cpMobile};

        var contactKeluarga = new ContactType(JenisContactEnum.Mobile, fs_telp_keluarga);
        var alamatKlg = new []{fs_alm1_keluarga, fs_alm2_keluarga, string.Empty};
        var addressKeluarga = new AlamatType(alamatKlg, fs_kota_pasien, fs_kd_pos_keluarga);
        var keluarga = new PasienKeluargaType(fs_nm_keluarga, fs_hub_keluarga, 
            contactKeluarga, addressKeluarga);
        
        //      status sosial
        var statusKawin = new StatusKawinDkType(fs_kd_status_kawin_dk, fs_nm_status_kawin_dk);
        var agama = new AgamaType(fs_kd_agama, fs_nm_agama);
        var suku = new SukuType(fs_kd_suku, fs_nm_suku);
        var pekerjaan = new PekerjaanDkType(fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk);
        var pendidikan = new PendidikanDkType(fs_kd_pendidikan_dk, fs_nm_pendidikan_dk);
    
        //      olah berkas
        var tglMedRec = fd_tgl_mr.ToDate(DateFormatEnum.YMD);

        var pasien = new PasienModel(fs_mr, fs_nm_pasien, tglLahir, gender,
            fs_nm_alias, fs_temp_lahir, fs_nm_ibu_kandung, golDarah,
            alamatDomisili, AlamatType.Default, kelurahan, identitas, identitasKk,
            listContact, keluarga, statusKawin, agama, suku, pekerjaan, pendidikan,
            tglMedRec, fb_aktif);

        return pasien;
    //
    }
}