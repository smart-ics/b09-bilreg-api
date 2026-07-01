CREATE TABLE ta_detil_tarif (
    fs_kd_detil_tarif VARCHAR(3) NOT NULL CONSTRAINT DF_ta_detil_tarif_fs_kd_detil_tarif DEFAULT (''),
    fs_nm_detil_tarif VARCHAR(30) NOT NULL CONSTRAINT DF_ta_detil_tafir_fs_nm_detil_tarif DEFAULT (''),
    fS_kd_grup_detil_tarif VARCHAR(2) NOT NULL CONSTRAINT DF_ta_detil_tarif_fs_kd_grup_detil_tarif DEFAULT(''),
    fs_kd_rek VARCHAR(20) NOT NULL CONSTRAINT DF_ta_detil_tarif_fs_kd_rek DEFAULT (''),
    fs_kd_rek_diskon VARCHAR(20) NOT NULL CONSTRAINT DF_ta_detil_tarif_fs_kd_rek_diskon DEFAULT (''),
    
    CONSTRAINT PK_ta_detil_tarif PRIMARY KEY CLUSTERED (fs_kd_detil_tarif)
)