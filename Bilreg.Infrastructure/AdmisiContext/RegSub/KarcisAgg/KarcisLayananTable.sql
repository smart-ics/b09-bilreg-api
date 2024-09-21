CREATE TABLE ta_karcis3 (
    fs_kd_karcis VARCHAR(2) NOT NULL CONSTRAINT DF_ta_karcis3_fs_kd_karcis DEFAULT (''),
    fs_kd_layanan VARCHAR(5) NOT NULL CONSTRAINT DF_ta_karcis3_fs_kd_layanan DEFAULT (''),

    INDEX IX_ta_karcis3_fs_kd_karcis CLUSTERED (fs_kd_karcis)
)