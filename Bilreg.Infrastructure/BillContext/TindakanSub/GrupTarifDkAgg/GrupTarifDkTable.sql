CREATE TABLE ta_grup_tarif_dk(
    fs_kd_grup_tarif_dk VARCHAR(2) NOT NULL CONSTRAINT DF_ta_grup_tarif_dk_fs_kd_grup_tarif_dk DEFAULT(''),
    fs_nm_grup_tarif_dk VARCHAR(20) NOT NULL CONSTRAINT DF_ta_grup_tarif_dk_fs_nm_grup_tarif_dk DEFAULT('')
        
    CONSTRAINT PK_TA_GRUP_TARIF_DK PRIMARY KEY (fs_kd_grup_tarif_dk)
)