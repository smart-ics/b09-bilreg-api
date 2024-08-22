CREATE TABLE ta_pekerjaan(
    fs_kd_pekerjaan_dk VARCHAR(3) NOT NULL CONSTRAINT DF_ta_pekerjaan_fs_kd_pekerjaan_dk DEFAULT(''),
    fs_nm_pekerjaan_dk VARCHAR(20) NOT NULL CONSTRAINT DF_ta_pekerjaan_fs_nm_pekerjaan_dk DEFAULT(''),

    CONSTRAINT PK_ta_pekerjaan PRIMARY KEY CLUSTERED(fs_kd_pekerjaan_dk)
)
