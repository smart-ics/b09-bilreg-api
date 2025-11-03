CREATE TABLE ta_karcis2 (
    fs_kd_karcis VARCHAR(2) NOT NULL CONSTRAINT DF_ta_karcis2_fs_kd_karcis DEFAULT (''),
    fs_kd_detil_tarif VARCHAR(3) NOT NULL CONSTRAINT DF_ta_karcis2_fs_kd_detil_tarif DEFAULT (''),
    fn_tarif DECIMAL(18, 0) NOT NULL CONSTRAINT DF_ta_karcis2_fn_tarif DEFAULT (0),
    
    INDEX IX_ta_karcis2_fs_kd_karcis CLUSTERED (fs_kd_karcis)
)