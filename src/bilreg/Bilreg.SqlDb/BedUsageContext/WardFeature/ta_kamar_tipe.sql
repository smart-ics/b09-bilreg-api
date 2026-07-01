CREATE TABLE ta_kamar_tipe (
    fs_kd_kamar_tipe VARCHAR(2) NOT NULL CONSTRAINT DF_ta_kamar_tipe_fs_kd_kamar_tipe DEFAULT(''),
    fs_nm_kamar_tipe VARCHAR(20) NOT NULL CONSTRAINT DF_ta_kamar_tipe_fs_nm_kamar_tipe DEFAULT(''),
    fb_gabung BIT NOT NULL CONSTRAINT DF_ta_kamar_tipe_fb_gabung DEFAULT(0),
    fb_aktif BIT NOT NULL CONSTRAINT DF_ta_kamar_tipe_fb_aktif DEFAULT(0),
    fb_default_tipe BIT NOT NULL CONSTRAINT DF_ta_kamar_tipe_fb_default_tipe DEFAULT(0),
    fb_no_urut INT NOT NULL CONSTRAINT DF_ta_kamar_tipe_fb_no_urut DEFAULT(0),
    CONSTRAINT PK_ta_kamar_tipe PRIMARY KEY (fs_kd_kamar_tipe)
