CREATE TABLE ta_registrasi3
(
    fs_kd_reg   VARCHAR(10) NOT NULL CONSTRAINT DF_ta_registrasi3_fs_kd_reg DEFAULT(''),
    fs_kd_bayar VARCHAR(5) NOT NULL CONSTRAINT DF_ta_registrasi3_fs_kd_bayar DEFAULT(''),
    fn_jasa     DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_registrasi3_fn_jasa DEFAULT(0),
    fn_obat     DECIMAL(18,0) NOT NULL CONSTRAINT DF_ta_registrasi3_fn_obat DEFAULT(0),
    fs_kd_rek   VARCHAR(20) NOT NULL CONSTRAINT DF_ta_registrasi3_fs_kd_rek DEFAULT(''),

    CONSTRAINT PK_ta_registrasi3 PRIMARY KEY CLUSTERED(fs_kd_reg)
)
GO
