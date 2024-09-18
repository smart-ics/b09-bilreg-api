CREATE TABLE ta_jenis_tarif (
    fs_kd_jenis_tarif VARCHAR(2) NOT NULL CONSTRAINT DC_fs_kd_jenis_tarif DEFAULT(''),
    fs_nm_jenis_tarif VARCHAR(30) NOT NULL CONSTRAINT DC_fs_nm_jenis_tarif DEFAULT(''),
    
    CONSTRAINT PK_ta_jenis_tarif PRIMARY KEY CLUSTERED (fs_kd_jenis_tarif)
);