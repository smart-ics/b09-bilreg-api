CREATE TABLE ta_rujukan (
    fs_kd_rujukan VARCHAR(10) NOT NULL CONSTRAINT DF_ta_rujukan_fs_kd_rujukan DEFAULT(''),
    fs_nm_rujukan VARCHAR(40) NOT NULL CONSTRAINT DF_ta_rujukan_fs_nm_rujukan DEFAULT(''),
    fb_aktif BIT NOT NULL CONSTRAINT DF_ta_rujukan_fb_aktif DEFAULT(1),
    fs_alm_rujukan VARCHAR(40) NOT NULL CONSTRAINT DF_ta_rujukan_fs_alm_rujukan DEFAULT(''),
    fs_alm2_rujukan VARCHAR(40) NULL CONSTRAINT DF_ta_rujukan_fs_alm2_rujukan DEFAULT(''),
    fs_kota_rujukan VARCHAR(20) NOT NULL CONSTRAINT DF_ta_rujukan_fs_kota_rujukan DEFAULT(''),
    fs_tlp_rujukan VARCHAR(30) NULL CONSTRAINT DF_ta_rujukan_fs_tlp_rujukan DEFAULT(''),
    fs_kd_rujukan_tipe VARCHAR(1) NOT NULL CONSTRAINT DF_ta_rujukan_fs_kd_rujukan_tipe DEFAULT(''),
    fs_kd_kelas_rs VARCHAR(1) NOT NULL CONSTRAINT DF_ta_rujukan_fs_kd_kelas_rs DEFAULT(''),
    fs_kd_cara_masuk_dk VARCHAR(1) NOT NULL CONSTRAINT DF_ta_rujukanfs_kd_cara_masuk_dk DEFAULT(''),

    CONSTRAINT PK_ta_rujukan PRIMARY KEY CLUSTERED (fs_kd_rujukan)
);
