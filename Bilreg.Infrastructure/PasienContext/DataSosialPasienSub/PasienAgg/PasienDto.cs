using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PasienContext.DataSosialPasienSub.PasienAgg;

internal class PasienDto() : PasienModel(string.Empty, string.Empty)
{
    public string fs_mr { get => PasienId; set => PasienId = value; }
    public string fs_nm_pasien {get => PasienName; set => PasienName = value;}
    public string fs_nm_alias { get => NickName; set => NickName = value;}

    public string fs_temp_lahir {get => TempatLahir; set => TempatLahir = value;}
    public string fd_tgl_lahir {get => TglLahir.ToString("yyyy-MM-dd"); set => TglLahir = value.ToDate(DateFormatEnum.YMD);}
    public string fs_jns_kelamin {get => Gender; set => Gender = value;}
    public string fd_tgl_mr {get => TglMedrec.ToString("yyyy-MM-dd"); set => TglMedrec = value.ToDate(DateFormatEnum.YMD);}
    public string fs_nm_ibu_kandung {get => IbuKandung; set => IbuKandung = value;}
    public string fs_gol_darah {get => GolDarah; set => GolDarah = value;}
    
    public string fs_kd_status_kawin_dk {get => StatusNikahId; set => StatusNikahId = value;}
    public string fs_nm_status_kawin_dk {get => StatusNikahName; set => StatusNikahName = value;}
    public string fs_kd_agama {get => AgamaId; set => AgamaId = value;}
    public string fs_nm_agama {get => AgamaName; set => AgamaName = value;}
    public string fs_kd_suku {get => SukuId; set => SukuId = value;}
    public string fs_nm_suku {get => SukuName; set => SukuName = value;}
    public string fs_kd_pekerjaan_dk {get =>PekerjaanDkId; set => PekerjaanDkId = value;}
    public string fs_nm_pekerjaan_dk {get => PekerjaanDkName; set => PekerjaanDkName = value;}
    public string fs_kd_pendidikan_dk {get => PendidikanDkId; set => PendidikanDkId = value;}
    public string fs_nm_pendidikan_dk {get => PendidikanDkName; set => PendidikanDkName = value;}
    
    public string fs_alm_pasien {get => Alamat; set => Alamat = value;}
    public string fs_alm2_pasien {get => Alamat2; set => Alamat2 = value;}
    public string fs_alm3_pasien {get => Alamat3; set => Alamat3 = value;}
    public string fs_kota_pasien {get => Kota; set => Kota = value;}
    public string fs_kd_pos_pasien {get => KodePos; set => KodePos = value;}
    public string fs_kd_kelurahan {get => KelurahanId; set => KelurahanId = value;}
    public string fs_nm_kelurahan {get => KelurahanName; set => KelurahanName = value;}
    public string fs_nm_kecamatan {get => KecamatanName; set => KecamatanName = value;}
    public string fs_nm_kabupaten {get => KabupatenName; set => KabupatenName = value;}
    public string fs_nm_propinsi {get => PropinsiName; set => PropinsiName = value;}
    public string fs_jenis_id {get => JenisId; set => JenisId = value;}
    public string fs_kd_identitas {get => NomorId; set => NomorId = value;}
    public string fs_no_kk {get => NomorKk; set => NomorKk = value;}
    
    public string fs_email {get => Email; set => Email = value;}
    public string fs_tlp_pasien {get => NoTelp; set => NoTelp = value;}
    public string fs_no_hp {get => NoHp; set => NoHp = value;}
    
    public string fs_nm_keluarga {get => KeluargaName; set => KeluargaName = value;}
    public string fs_hub_keluarga {get => KeluargaRelasi; set => KeluargaRelasi = value;}
    public string fs_telp_keluarga {get => KeluargaNoTelp; set => KeluargaNoTelp = value;}
    public string fs_alm1_keluarga {get => KeluargaAlamat1; set => KeluargaAlamat1 = value;}
    public string fs_alm2_keluarga {get => KeluargaAlamat2; set => KeluargaAlamat2 = value;}
    public string fs_kota_keluarga {get => KeluargaKota; set => KeluargaKota = value;}
    public string fs_kd_pos_keluarga {get => KeluargaKodePos; set => KeluargaKodePos = value;}
}