CREATE TABLE TA_REGISTRASI2(
    fs_kd_reg VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi2_fs_kd_reg DEFAULT(''),
    fs_kd_detil_tarif VARCHAR (3) NOT NULL CONSTRAINT DF_ta_registrasi2_fs_kd_detil_tarif DEFAULT(''),
    fn_tarif DECIMAL (18, 0) NOT NULL CONSTRAINT DF_ta_registrasi2_fn_tarif DEFAULT(0),
    fn_diskon DECIMAL (18, 0) NOT NULL CONSTRAINT DF_ta_registrasi2_fn_diskon DEFAULT(0),
    fs_kd_petugas_medis VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi2_fs_kd_petugas_medis DEFAULT(''),
    fb_void BIT NOT NULL CONSTRAINT DF_ta_registrasi2_fb_void DEFAULT(0)
)
GO

CREATE CLUSTERED INDEX CX_ta_registrasi2_fs_kd_reg
       ON ta_registrasi2(fs_kd_reg)
GO
