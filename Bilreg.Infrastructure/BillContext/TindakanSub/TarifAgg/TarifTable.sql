CREATE TABLE ta_tarif (
    fs_kd_tarif VARCHAR(10) NOT NULL CONSTRAINT DF_ta_tarif_fs_kd_tarif DEFAULT (''),
    fs_nm_tarif VARCHAR(55) NOT NULL CONSTRAINT DF_ta_tarif_fs_nm_tarif DEFAULT (''),
    
    CONSTRAINT PK_ta_tarif PRIMARY KEY CLUSTERED (fs_kd_tarif)
)