CREATE TABLE ta_map_jmn_jk
(
    fs_kd_jaminan VARCHAR(50) NOT NULL CONSTRAINT DF_ta_map_jmn_jk_fs_kd_jaminan DEFAULT(''),
    fs_kd_jk VARCHAR(50) NOT NULL CONSTRAINT DF_ta_map_jmn_jk_fs_kd_jk DEFAULT(''),

    CONSTRAINT PK_ta_map_jmn_jk PRIMARY KEY CLUSTERED(fs_kd_jaminan, fs_kd_jk)
)