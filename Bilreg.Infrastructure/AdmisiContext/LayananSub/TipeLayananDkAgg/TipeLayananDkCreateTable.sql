CREATE TABLE ta_layanan_tipe_dk(
    fs_kd_layanan_tipe_dk VARCHAR(2) NOT NULL CONSTRAINT DF_ta_layanan_tipe_dk_fs_kd_layanan_tipe_dk DEFAULT(''),
    fs_nm_layanan_tipe_dk VARCHAR(20) NOT NULL CONSTRAINT DF_ta_layanan_tipe_dk_fs_nm_layanan_tipe_dk DEFAULT(''),

    CONSTRAINT PK_ta_layanan_tipe_dk PRIMARY KEY CLUSTERED(fs_kd_layanan_tipe_dk)
)