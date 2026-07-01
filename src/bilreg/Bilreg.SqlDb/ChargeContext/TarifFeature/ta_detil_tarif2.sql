CREATE TABLE ta_detil_tarif2(
    fs_kd_detil_tarif VARCHAR(3) NOT NULL CONSTRAINT DF_ta_detil_tarif2_fs_kd_detil_tarif DEFAULT(''),
    fs_kd_sat_tugas VARCHAR(3) NOT NULL CONSTRAINT DF_ta_detil_tarif2_fs_kd_sat_tugas DEFAULT(''),

    CONSTRAINT PK_ta_detil_tarif2 PRIMARY KEY CLUSTERED (fs_kd_detil_tarif, fs_kd_sat_tugas)
)