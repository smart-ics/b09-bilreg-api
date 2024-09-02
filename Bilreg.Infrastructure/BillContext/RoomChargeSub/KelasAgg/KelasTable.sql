CREATE TABLE ta_kelas (
    fs_kd_kelas VARCHAR(3) NOT NULL CONSTRAINT DF_ta_kelas_fs_kd_kelas DEFAULT(''),
    fs_nm_kelas VARCHAR(30) NOT NULL CONSTRAINT DF_ta_kelas_fs_nm_kelas DEFAULT(''),
    fs_kd_kelas_dk VARCHAR(1) NOT NULL CONSTRAINT DF_ta_kelas_fs_kd_kelas_dk DEFAULT(''),
    fb_aktif BIT NOT NULL CONSTRAINT DF_ta_kelas_fb_aktif DEFAULT(0),
    
    CONSTRAINT PK_ta_kelas PRIMARY KEY CLUSTERED (fs_kd_kelas)
);
