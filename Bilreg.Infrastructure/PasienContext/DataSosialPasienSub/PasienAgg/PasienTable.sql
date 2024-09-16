CREATE TABLE tc_mr(
    fs_mr VARCHAR(15) NOT NULL CONSTRAINT DF_tc_mr_fs_mr DEFAULT(''),
    fs_nm_pasien VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_nm_pasien DEFAULT(''),
    fs_nm_alias VARCHAR(20) NOT NULL CONSTRAINT DF_tc_mr_fs_nm_alias DEFAULT(''),

    fs_temp_lahir VARCHAR(20) NOT NULL CONSTRAINT DF_tc_mr_fs_temp_lahir DEFAULT(''),
    fd_tgl_lahir VARCHAR(10) NOT NULL CONSTRAINT DF_tc_mr_td_tgl_lahir DEFAULT('3000-01-01'),
    fs_jns_kelamin VARCHAR(1) NOT NULL CONSTRAINT DF_tc_mr_fs_jns_kelamin DEFAULT(''),
    fd_tgl_mr VARCHAR(10) NOT NULL CONSTRAINT DF_tc_mr_fd_tgl_mr DEFAULT('3000-01-01'),
    fs_nm_ibu_kandung VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_nm_ibu_kandung DEFAULT(''),
    fs_gol_darah VARCHAR(3) NOT NULL CONSTRAINT DF_tc_mr_fs_gol_darah DEFAULT(''),
    
    fs_kd_status_kawin_dk VARCHAR(1) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_status_kawin_dk DEFAULT(''),
    fs_kd_agama VARCHAR(1) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_agama DEFAULT(''),
    fs_kd_suku VARCHAR(3) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_suku DEFAULT(''),
    fs_kd_pekerjaan_dk VARCHAR(3) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_pekerjaan_dk DEFAULT(''),
    fs_kd_pendidikan_dk VARCHAR(1) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_pendidikan_dk DEFAULT(''),

    fs_alm_pasien VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_alm_pasien DEFAULT(''),
    fs_alm2_pasien VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_alm2_pasien DEFAULT(''),
    fs_alm3_pasien VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_alm3_pasien DEFAULT(''),
    fs_kota_pasien VARCHAR(20) NOT NULL CONSTRAINT DF_tc_mr_fs_kota_pasien DEFAULT(''),
    fs_kd_pos_pasien VARCHAR(6) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_pos_pasien DEFAULT(''),
    fs_kd_kelurahan VARCHAR(10) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_kelurahan DEFAULT(''),

    fs_jenis_id VARCHAR(6) NOT NULL CONSTRAINT DF_tc_mr_fs_jenis_id DEFAULT(''),
    fs_kd_identitas VARCHAR(30) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_identitas DEFAULT(''),
    fs_no_kk VARCHAR(30) NOT NULL CONSTRAINT DF_tc_mr_fs_no_kk DEFAULT(''),

    fs_email VARCHAR(64) NOT NULL CONSTRAINT DF_tc_mr_fs_email DEFAULT(''),
    fs_tlp_pasien VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_tlp_pasien DEFAULT(''),
    fs_no_hp VARCHAR(30) NOT NULL CONSTRAINT DF_tc_mr_fs_no_hp DEFAULT('')
    
    CONSTRAINT PK_tc_mr PRIMARY KEY CLUSTERED (fs_mr)
)


-- ** FS_MR
-- ** FS_NM_PASIEN
-- ** FS_NM_ALIAS
-- ** FS_TEMP_LAHIR
-- ** FD_TGL_LAHIR
-- ** FS_JNS_KELAMIN
-- ** FD_TGL_MR
-- ** FS_NM_IBU_KANDUNG
-- ** FS_GOL_DARAH
-- ** FS_KD_STATUS_KAWIN_DK
-- ** FS_KD_AGAMA
-- ** FS_KD_SUKU
-- ** FS_KD_PEKERJAAN_DK
-- ** FS_KD_PENDIDIKAN_DK

-- ** FS_ALM_PASIEN
-- ** FS_ALM2_PASIEN
-- ** FS_ALM3_PASIEN
-- ** FS_KOTA_PASIEN
-- ** FS_KD_POS_PASIEN
-- ** FS_KD_KELURAHAN

-- ** FS_JENIS_ID
-- ** FS_KD_IDENTITAS
-- ** fs_no_kk

-- ** FS_EMAIL
-- ** FS_TLP_PASIEN
-- ** FS_NO_HP


-- FS_MR_IBU
-- FS_KD_PETUGAS
-- FS_NM_KELUARGA
-- FS_HUB_KELUARGA
-- FS_TELP_KELUARGA
-- FS_ALM1_KELUARGA
-- FS_ALM2_KELUARGA
-- FS_KOTA_KELUARGA
-- FS_KD_POS_KELUARGA
-- FD_TGL_KUNJUNGAN_AKHIR
-- FD_TGL_MATI
-- FI_PICTURE
-- FN_KUNJUNGAN_KE
-- FS_ALERGI_MAKANAN
-- FS_ALERGI_OBAT
-- FS_LAYANAN_TERAKHIR
-- FS_WARGA_NEGARA
-- FS_JAM_LAHIR
-- FS_MR_LAMA
-- FS_MR_GENERATE
-- FS_KD_PEG
-- FN_CETAK_RM01
-- FN_CETAK_LABEL
-- FB_AKTIF
-- FD_TGL_ENTRY
-- FS_JAM_ENTRY
-- FS_KD_PTG_ENTRY
-- FS_NO_FAX
-- FS_MR_FILM
-- FN_CETAK_GELANG
-- FN_STATUS_NU
-- FN_STATUS_HSD
-- FS_MR_INDUK
-- FS_KD_PEKERJAAN_KELUARGA_DK
-- FS_KD_IDENTITAS_KELUARGA
-- FS_KD_NEGARA
-- FN_CETAK_KARTU
-- FS_KD_MARKET_REFERRAL
-- FN_BERAT_LAHIR
-- FS_NO_NPWP
-- fd_tgl_hpl_terakhir
-- fs_kd_perusahaan
-- FS_KD_BAHASA