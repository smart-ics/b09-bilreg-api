CREATE TABLE ta_rujukan_tipe(
    fs_kd_rujukan_tipe VARCHAR(1) NOT NULL CONSTRAINT DF_ta_rujukan_tipe_fs_kd_rujukan_tipe DEFAULT(''),
    fs_nm_rujukan_tipe VARCHAR(20) NOT NULL CONSTRAINT DF_ta_rujukan_tipe_fs_nm_rujukan_tipe DEFAULT(''),

    CONSTRAINT PK_ta_rujukan_tipe PRIMARY KEY CLUSTERED(fs_kd_rujukan_tipe)
)
