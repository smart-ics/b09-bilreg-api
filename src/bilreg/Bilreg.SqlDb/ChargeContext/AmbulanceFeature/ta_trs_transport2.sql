CREATE TABLE ta_trs_transport2 (
    fs_kd_trs VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_transport2_fs_kd_trs DEFAULT (''),
    fs_kd_detil_tarif VARCHAR(3) NOT NULL CONSTRAINT DF_ta_trs_transport2_fs_kd_detil_tarif DEFAULT (''),
    fn_tarif DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport2_fn_tarif DEFAULT (0),
    fn_qty DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport2_fn_qty DEFAULT (1),
    fs_kd_petugas_medis VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_transport2_fs_kd_petugas_medis DEFAULT (''),
    fn_diskon DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport2_fn_diskon DEFAULT (0),
    fn_sub_total DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_transport2_fn_sub_total DEFAULT (0),
    fb_void BIT NOT NULL CONSTRAINT DF_ta_trs_transport2_fb_void DEFAULT (0),
    fs_keterangan VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_transport2_fs_keterangan DEFAULT (''),

    CONSTRAINT PK_ta_trs_transport2 PRIMARY KEY CLUSTERED (fs_kd_trs, fs_kd_detil_tarif)
)
GO

CREATE INDEX IX_ta_trs_transport2_fs_kd_trs
    ON ta_trs_transport2(fs_kd_trs)
GO
