CREATE TABLE ta_pekerjaan_dk(
    fs_kd_pekerjaan_dk VARCHAR(3) NOT NULL CONSTRAINT DF_ta_pekerjaan_dk_fs_kd_pekerjaan_dk DEFAULT(''),
    fs_nm_pekerjaan_dk VARCHAR(30) NOT NULL CONSTRAINT DF_ta_pekerjaan_dk_fs_nm_pekerjaan_dk DEFAULT(''),

    CONSTRAINT PK_ta_pekerjaan_dk PRIMARY KEY CLUSTERED(fs_kd_pekerjaan_dk)
)
