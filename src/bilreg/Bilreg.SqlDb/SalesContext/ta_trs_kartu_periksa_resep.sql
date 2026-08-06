CREATE TABLE ta_trs_kartu_periksa_resep
(
    fs_kd_trs          VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa_resep_fs_kd_trs DEFAULT(''),
    fs_catatan_resep   VARCHAR(512) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa_resep_fs_catatan_resep DEFAULT(''),

    CONSTRAINT PK_ta_trs_kartu_periksa_resep PRIMARY KEY CLUSTERED(fs_kd_trs)
)
