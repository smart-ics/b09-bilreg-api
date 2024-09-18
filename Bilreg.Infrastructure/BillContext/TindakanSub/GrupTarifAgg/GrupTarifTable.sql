CREATE TABLE ta_grup_tarif (
    fs_kd_grup_tarif VARCHAR(2)  NOT NULL CONSTRAINT DF_ta_grup_tarif_fs_kd_grup_tarif DEFAULT (''),
    fs_nm_grup_tarif VARCHAR(30) NOT NULL CONSTRAINT DF_ta_grup_tarif_fs_nm_grup_tarif DEFAULT (''),
    
    CONSTRAINT PK_ta_grup_tarif PRIMARY KEY CLUSTERED (fs_kd_grup_tarif)
)