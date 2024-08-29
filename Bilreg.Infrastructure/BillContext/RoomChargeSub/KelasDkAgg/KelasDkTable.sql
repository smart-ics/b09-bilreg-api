CREATE TABLE ta_kelas_dk(
    fs_kd_kelas_dk VARCHAR(1) NOT NULL CONSTRAINT DF_ta_kelas_dk_fs_kd_kelas_dk DEFAULT(''),
    fs_nm_kelas_dk VARCHAR(15) NOT NULL CONSTRAINT DF_ta_kelas_dk_fs_nm_kelas_dk DEFAULT(''),

    CONSTRAINT PK_ta_kelas_dk PRIMARY KEY CLUSTERED(fs_kd_kelas_dk)
)