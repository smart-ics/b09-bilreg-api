using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using Nuna.Lib.ValidationHelper;

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

    public PasienModel ToPasienModel()
    {
        var tglLahir = fd_tgl_lahir.ToDate(DateFormatEnum.YMD);
        var gender = new GenderType(fs_jns_kelamin);
        var pasien = new PasienModel(fs_mr, fs_nm_pasien, tglLahir, gender);

        var golDarah = new GolDarahType(fs_gol_darah);
        pasien.SetPersonalInfo(fs_nm_alias, fs_temp_lahir, fs_nm_ibu_kandung, golDarah);

        var address = new AddressType(fs_alm_pasien, fs_alm2_pasien, 
            fs_alm3_pasien, fs_kota_pasien, fs_kd_pos_pasien);
        var propinsi = new PropinsiModel(fs_kd_propinsi, fs_nm_propinsi);
        var kabupaten = new KabupatenModel(fs_kd_kabupaten, fs_nm_kabupaten, propinsi);
        var kecamatan = new KecamatanModel(fs_kd_kecamatan, fs_nm_kecamatan, kabupaten);
        var kelurahan = new KelurahanModel(fs_kd_kelurahan, fs_nm_kelurahan, 
            fs_kd_pos_pasien, kecamatan);
        var identitas = new IdentityType(fs_jenis_id, fs_kd_identitas, fs_no_kk);
        var contact = new ContactType(fs_email, fs_tlp_pasien, fs_no_hp);
        var contactKeluarga = new ContactType("", fs_telp_keluarga, "");
        var addressKeluarga = new AddressType(fs_alm1_keluarga, fs_alm2_keluarga, "", 
                fs_kota_pasien, fs_kd_pos_keluarga);
        var keluarga = new KeluargaType(fs_nm_keluarga, fs_hub_keluarga, 
            contactKeluarga, addressKeluarga);
        pasien.SetAdministrativeInfo(address, kelurahan, identitas, contact, keluarga);
        
        return pasien;
    }
}