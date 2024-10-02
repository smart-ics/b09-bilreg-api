CREATE TABLE ta_tarif_tipe (
    fs_kd_tarif_tipe VARCHAR(2) NOT NULL CONSTRAINT DF_ta_tarif_tipe_fs_kd_tarif_tipe DEFAULT(''),
    fs_nm_tarif_tipe VARCHAR(30) NOT NULL CONSTRAINT DF_ta_tarif_tipe_fs_nm_tarif_tipe DEFAULT(''),
    fb_aktif BIT NOT NULL CONSTRAINT DF_ta_tarif_tipe_fb_aktif DEFAULT(0),
    fs_no_urut DECIMAL(18) NOT NULL CONSTRAINT DF_ta_tarif_tipe_fs_no_urut DEFAULT(0),

    CONSTRAINT PK_ta_tarif_tipe PRIMARY KEY CLUSTERED (fs_kd_tarif_tipe)
);
