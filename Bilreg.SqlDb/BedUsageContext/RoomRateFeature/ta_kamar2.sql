CREATE TABLE ta_kamar2
(
    fs_kd_kamar      VARCHAR(5) NOT NULL CONSTRAINT DF_ta_kamar2_fs_kd_kamar DEFAULT(''),
    fs_kd_detil_tarif VARCHAR(3) NOT NULL CONSTRAINT DF_ta_kamar2_fs_kd_detil_tarif DEFAULT(''),
    fs_kd_tipe_kamar VARCHAR(2) NOT NULL CONSTRAINT DF_ta_kamar2_fs_kd_tipe_kamar DEFAULT(''),
    fn_tarif         DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_kamar2_fn_tarif DEFAULT(0),
    fn_no_urut       INT NOT NULL CONSTRAINT DF_ta_kamar2_fn_no_urut DEFAULT(0),
    fs_kd_kelas      VARCHAR(10) NOT NULL CONSTRAINT DF_ta_kamar2_fs_kd_kelas DEFAULT(''),
    fn_harike        INT NOT NULL CONSTRAINT DF_ta_kamar2_fn_harike DEFAULT(0),

    CONSTRAINT PK_ta_kamar2 PRIMARY KEY CLUSTERED(fs_kd_kamar, fs_kd_detil_tarif, fn_no_urut)
)