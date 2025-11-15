CREATE TABLE ta_detil_tarif (
    fs_kd_detil_tarif VARCHAR(3) NOT NULL CONSTRAINT DF_ta_detil_tarif_fs_kd_detil_tarif DEFAULT (''),
    fs_nm_detil_tarif VARCHAR(30) NOT NULL CONSTRAINT DF_ta_detil_tafir_fs_nm_detil_tarif DEFAULT (''),
    
    CONSTRAINT PK_ta_detil_tarif PRIMARY KEY CLUSTERED (fs_kd_detil_tarif)
)