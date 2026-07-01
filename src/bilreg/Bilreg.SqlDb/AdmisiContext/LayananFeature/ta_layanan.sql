CREATE TABLE ta_layanan(
    fs_kd_layanan VARCHAR(5) NOT NULL CONSTRAINT DF_ta_layanan_fs_kd_layanan DEFAULT(''),
    fs_nm_layanan VARCHAR(30) NOT NULL CONSTRAINT DF_ta_layanan_fs_nm_layanan DEFAULT(''),
    fb_aktif BIT NOT NULL CONSTRAINT DF_ta_layanan_fb_aktif DEFAULT(0),
    fs_kd_instalasi VARCHAR(5) NOT NULL CONSTRAINT DF_ta_layanan_fs_kd_instalasi DEFAULT(''),
    fs_kd_layanan_dk VARCHAR(5) NOT NULL CONSTRAINT DF_ta_layanan_fs_kd_layanan_dk DEFAULT(''),
    fs_kd_layanan_tipe_dk VARCHAR(5) NOT NULL CONSTRAINT DF_ta_layanan_fs_kd_layanan_tipe_dk DEFAULT(''),
    
    CONSTRAINT PK_ta_layanan PRIMARY KEY CLUSTERED(fs_kd_layanan)
)