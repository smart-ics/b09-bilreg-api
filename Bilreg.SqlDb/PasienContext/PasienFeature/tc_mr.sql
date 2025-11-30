CREATE TABLE tc_mr(
    fs_mr VARCHAR(15) NOT NULL CONSTRAINT DF_tc_mr_fs_mr DEFAULT(''),
    fs_nm_pasien VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_nm_pasien DEFAULT(''),
    fd_tgl_lahir VARCHAR(10) NOT NULL CONSTRAINT DF_tc_mr_td_tgl_lahir DEFAULT('3000-01-01'),
    fs_jns_kelamin VARCHAR(1) NOT NULL CONSTRAINT DF_tc_mr_fs_jns_kelamin DEFAULT(''),
    
    fs_nm_alias VARCHAR(20) NOT NULL CONSTRAINT DF_tc_mr_fs_nm_alias DEFAULT(''),
    fs_temp_lahir VARCHAR(20) NOT NULL CONSTRAINT DF_tc_mr_fs_temp_lahir DEFAULT(''),
    fs_nm_ibu_kandung VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_nm_ibu_kandung DEFAULT(''),
    fs_gol_darah VARCHAR(3) NOT NULL CONSTRAINT DF_tc_mr_fs_gol_darah DEFAULT(''),
    
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
    fs_no_hp VARCHAR(30) NOT NULL CONSTRAINT DF_tc_mr_fs_no_hp DEFAULT(''),
    
    fs_nm_keluarga VARCHAR(35) NOT NULL CONSTRAINT DF_tc_mr_fs_nm_keluarga DEFAULT (''),
    fs_hub_keluarga VARCHAR(35) NOT NULL CONSTRAINT DF_tc_mr_fs_hub_keluarga DEFAULT (''),
    fs_telp_keluarga VARCHAR(35) NOT NULL CONSTRAINT DF_tc_mr_fs_telp_keluarga DEFAULT (''),
    fs_alm1_keluarga VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_alm1_keluarga DEFAULT (''),
    fs_alm2_keluarga VARCHAR(40) NOT NULL CONSTRAINT DF_tc_mr_fs_alm2_keluarga DEFAULT (''),
    fs_kota_keluarga VARCHAR(20) NOT NULL CONSTRAINT DF_tc_mr_fs_kota_keluarga DEFAULT (''),
    fs_kd_pos_keluarga VARCHAR(5) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_pos_keluarga DEFAULT (''),
    
    fs_kd_status_kawin_dk VARCHAR(1) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_status_kawin_dk DEFAULT(''),
    fs_kd_agama VARCHAR(1) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_agama DEFAULT(''),
    fs_kd_suku VARCHAR(3) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_suku DEFAULT(''),
    fs_kd_pekerjaan_dk VARCHAR(3) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_pekerjaan_dk DEFAULT(''),
    fs_kd_pendidikan_dk VARCHAR(1) NOT NULL CONSTRAINT DF_tc_mr_fs_kd_pendidikan_dk DEFAULT(''),
    
    fd_tgl_mr VARCHAR(10) NOT NULL CONSTRAINT DF_tc_mr_fd_tgl_mr DEFAULT('3000-01-01'),
    fb_aktif BIT NOT NULL CONSTRAINT DF_tc_mr_fb_aktif DEFAULT(0)
    
    CONSTRAINT PK_tc_mr PRIMARY KEY CLUSTERED (fs_mr)
)
GO

CREATE FULLTEXT CATALOG PatientCatalog AS DEFAULT;
GO

CREATE FULLTEXT INDEX ON tc_mr(fs_nm_pasien)
KEY INDEX IDX_FS_MR
WITH STOPLIST = SYSTEM;
GO
       