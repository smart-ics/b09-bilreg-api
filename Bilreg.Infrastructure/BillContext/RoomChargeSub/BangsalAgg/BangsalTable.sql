CREATE TABLE ta_bangsal(
    fs_kd_bangsal VARCHAR(2) NOT NULL CONSTRAINT DF_ta_bangsal_fs_kd_bangsal DEFAULT(''),
    fs_nm_bangsal VARCHAR(30) NOT NULL CONSTRAINT DF_ta_bangsal_fs_nm_bangsal DEFAULT(''),
    fs_kd_layanan VARCHAR(5) NOT NULL CONSTRAINT DF_ta_bangsal_fs_kd_layanan DEFAULT(''),

    CONSTRAINT PK_ta_bangsal PRIMARY KEY CLUSTERED (fs_kd_bangsal)
);