CREATE TABLE ta_trs_billing
(
    fs_kd_trs VARCHAR(26) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_kd_trs DEFAULT(''),
    fn_modul INT NOT NULL CONSTRAINT DF_ta_trs_billing_fn_modul DEFAULT(0),
    fd_tgl_trs VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_billing_fd_tgl_trs DEFAULT('3000-01-01'),
    fs_jam_trs VARCHAR(8) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_jam_trs DEFAULT('00:00:00'),
    fs_kd_reg VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_kd_reg DEFAULT(''),
    fs_kd_layanan VARCHAR(5) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_kd_layanan DEFAULT(''),
    fs_kd_kelas VARCHAR(3) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_kd_kelas DEFAULT(''),
    fs_kd_petugas VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_kd_petugas DEFAULT(''),
    fn_sub_total DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_billing_fn_sub_total DEFAULT(0),
    fn_diskon DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_billing_fn_diskon DEFAULT(0),
    fn_biaya DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_billing_fn_biaya DEFAULT(0),
    fn_tax DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_billing_fn_tax DEFAULT(0),
    fn_total DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_billing_fn_total DEFAULT(0),
    fs_keterangan VARCHAR(255) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_keterangan DEFAULT(''),
    fs_keterangan2 VARCHAR(255) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_keterangan2 DEFAULT(''),
    fs_kd_rekap_cetak VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_kd_rekap_cetak DEFAULT(''),
    fs_kd_ref_biaya VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_kd_ref_biaya DEFAULT(''),
    fn_qty INT NOT NULL CONSTRAINT DF_ta_trs_billing_fn_qty DEFAULT(0),
    fs_kd_trs_main VARCHAR(26) NOT NULL CONSTRAINT DF_ta_trs_billing_fs_kd_trs_main DEFAULT(''),

    CONSTRAINT PK_ta_trs_billing PRIMARY KEY CLUSTERED(fs_kd_trs)
)