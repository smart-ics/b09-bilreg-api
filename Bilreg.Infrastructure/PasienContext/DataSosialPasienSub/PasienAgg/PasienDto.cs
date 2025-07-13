using Bilreg.Domain.PasienContext;
using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using Nuna.Lib.ValidationHelper;
using SukuType = Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg.SukuType;

namespace Bilreg.Infrastructure.PasienContext.DataSosialPasienSub.PasienAgg;

internal class PasienDto 
{
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

    public PasienModel ToModel()
    {
        //      personal info
        var tglLahir = fd_tgl_lahir.ToDate(DateFormatEnum.YMD);
        var gender = new GenderType(fs_jns_kelamin);
        var pasien = new PasienModel(fs_mr, fs_nm_pasien, tglLahir, gender);
        var golDarah = new GolDarahType(fs_gol_darah);
        pasien.SetPersonalInfo(fs_nm_alias, fs_temp_lahir, fs_nm_ibu_kandung, golDarah);

        //      administrative  
        var address = new AlamatType(fs_alm_pasien, fs_alm2_pasien, 
            fs_alm3_pasien, fs_kota_pasien, fs_kd_pos_pasien);
        var propinsi = new PropinsiType(fs_kd_propinsi, fs_nm_propinsi);
        var kabupaten = new KabupatenType(fs_kd_kabupaten, fs_nm_kabupaten, propinsi);
        var kecamatan = new KecamatanType(fs_kd_kecamatan, fs_nm_kecamatan, kabupaten);
        var kelurahan = new KelurahanModel(fs_kd_kelurahan, fs_nm_kelurahan, 
            fs_kd_pos_pasien, kecamatan);
        var identitas = new IdentitasType(fs_jenis_id, fs_kd_identitas, fs_no_kk);
        var contact = new ContactType(fs_email, fs_tlp_pasien, fs_no_hp);
        var contactKeluarga = new ContactType(string.Empty, fs_telp_keluarga, string.Empty);
        var addressKeluarga = new AlamatType(fs_alm1_keluarga, fs_alm2_keluarga, string.Empty, 
                fs_kota_pasien, fs_kd_pos_keluarga);
        var keluarga = new PasienKeluargaType(fs_nm_keluarga, fs_hub_keluarga, 
            contactKeluarga, addressKeluarga);
        pasien.SetAdministrativeInfo(address, kelurahan, identitas, contact, keluarga);
        
        //      status sosial
        var statusKawin = new StatusKawinDkModel(fs_kd_status_kawin_dk, fs_nm_status_kawin_dk);
        var agama = new AgamaModel(fs_kd_agama, fs_nm_agama);
        var suku = new SukuType(fs_kd_suku, fs_nm_suku);
        var pekerjaan = new PekerjaanDkModel(fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk);
        var pendidikan = new PendidikanDkModel(fs_kd_pendidikan_dk, fs_nm_pendidikan_dk);
        pasien.SetStatusSosial(statusKawin, agama, suku, pekerjaan, pendidikan);

        //      olah berkas
        var tglMedRec = fd_tgl_mr.ToDate(DateFormatEnum.YMD);
        pasien.SetTglMedRec(tglMedRec);
        
        return pasien;
    }
}