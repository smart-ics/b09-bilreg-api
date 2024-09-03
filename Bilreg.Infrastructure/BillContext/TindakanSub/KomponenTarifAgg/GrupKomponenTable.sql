CREATE TABLE ta_grup_detil_tarif (
    fs_kd_grup_detil_tarif VARCHAR(12) NOT NULL CONSTRAINT DF_ta_grup_detil_tarif_fs_kd_grup_detil_tarif DEFAULT (''),
    fs_nm_grup_detil_tarif VARCHAR(12) NOT NULL CONSTRAINT DF_ta_grup_detil_tarif_fs_nm_grup_detil_tarif DEFAULT (''),
    
    CONSTRAINT PK_ta_grup_detil_tarif PRIMARY KEY CLUSTERED (fs_kd_grup_detil_tarif)
)