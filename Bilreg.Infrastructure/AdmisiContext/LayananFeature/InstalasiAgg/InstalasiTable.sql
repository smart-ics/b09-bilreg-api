CREATE TABLE ta_instalasi (
    fs_kd_instalasi VARCHAR(2) NOT NULL CONSTRAINT DF_ta_instalasi_fs_kd_instalasi DEFAULT(''),
    fs_nm_instalasi VARCHAR(30) NOT NULL CONSTRAINT DF_ta_instalasi_fs_nm_instalasi DEFAULT(''),
    fs_kd_instalasi_dk VARCHAR(1) NOT NULL CONSTRAINT DF_ta_instalasi_fs_kd_instalasi_dk DEFAULT(''),

    CONSTRAINT PK_ta_instalasi PRIMARY KEY CLUSTERED(fs_kd_instalasi)
)