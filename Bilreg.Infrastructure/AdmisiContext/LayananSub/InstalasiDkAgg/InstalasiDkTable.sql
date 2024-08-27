CREATE TABLE ta_instalasi_dk(
    fs_kd_instalasi_dk VARCHAR(1) NOT NULL CONSTRAINT DF_ta_instalasi_dk_fs_kd_instalasi_dk DEFAULT(''),
    fs_nm_instalasi_dk VARCHAR(30) NOT NULL CONSTRAINT DF_ta_instalasi_dk_fs_nm_instalasi_dk DEFAULT(''),

    CONSTRAINT PK_ta_instalasi_dk PRIMARY KEY CLUSTERED(fs_kd_instalasi_dk)
)