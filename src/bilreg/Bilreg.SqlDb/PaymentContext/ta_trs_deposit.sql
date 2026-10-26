CREATE TABLE ta_trs_deposit(
    fs_kd_trs VARCHAR(15) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs DEFAULT(''),
    fd_tgl_trs VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fd_tgl_trs DEFAULT('3000-01-01'),
    fs_jam_trs VARCHAR(8) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_jam_trs DEFAULT('00:00:00'),
    fd_tgl_jam_trs VARCHAR(19) NOT NULL CONSTRAINT DF_ta_trs_deposit_fd_tgl_jam_trs DEFAULT('3000-01-01 00:00:00'),
    fs_kd_petugas VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_petugas DEFAULT(''),
    fs_kd_reg VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_reg DEFAULT(''),
    fs_kd_layanan VARCHAR(5) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_layanan DEFAULT(''),
    fs_no_kwitansi VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_no_kwitansi DEFAULT(''),
    fs_keterangan VARCHAR(40) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_keterangan DEFAULT(''),
    fd_tgl_void VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fd_tgl_void DEFAULT('3000-01-01'),
    fs_jam_void VARCHAR(8) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_jam_void DEFAULT('00:00:00'),
    fs_kd_petugas_void VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_petugas_void DEFAULT(''),
    fn_kas DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_kas DEFAULT(0),
    fs_kd_rek_kas VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_rek_kas DEFAULT(''),
    fn_bank DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_bank DEFAULT(0),
    fs_kd_rek_bank VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_rek_bank DEFAULT(''),
    fn_kartu_kredit DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_kartu_kredit DEFAULT(0),
    fs_kd_rek_kartu_kredit VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_rek_kartu_kredit DEFAULT(''),
    fs_no_kartu_kredit VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_no_kartu_kredit DEFAULT(''),
    fs_kd_rek_deposit VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_rek_deposit DEFAULT(''),
    fs_kd_petugas_kasir VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_petugas_kasir DEFAULT(''),
    fs_terima_dari VARCHAR(40) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_terima_dari DEFAULT(''),
    fs_kd_trs_kwitansi VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs_kwitansi DEFAULT(''),
    fs_kd_trs_gen VARCHAR(15) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs_gen DEFAULT(''),
    fs_kd_jenis_bank VARCHAR(5) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_jenis_bank DEFAULT(''),
    fs_kd_jenis_kartu_kredit VARCHAR(5) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_jenis_kartu_kredit DEFAULT(''),
    fs_no_bank VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_no_bank DEFAULT(''),
    fs_kd_trs_deposit_khusus VARCHAR(15) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs_deposit_khusus DEFAULT(''),
    fs_kd_trs_voucher VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs_voucher DEFAULT(''),
    fn_nilai_deposit DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_nilai_deposit DEFAULT(0),
    fn_nilai_materai DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_nilai_materai DEFAULT(0),
    fn_total_bayar DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_total_bayar DEFAULT(0),
    fs_kd_trs_materai VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs_materai DEFAULT(''),
    fs_kd_trs_charge VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs_charge DEFAULT(''),
    fs_referensi VARCHAR(15) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_referensi DEFAULT(''),
    fs_kd_trs_voucher_dep VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs_voucher_dep DEFAULT(''),
    fn_total_deposit DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_total_deposit DEFAULT(0),
    fn_nilai_voucher_dep DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_nilai_voucher_dep DEFAULT(0),
    crttgl VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_crttgl DEFAULT('3000-01-01'),
    crtjam VARCHAR(8) NOT NULL CONSTRAINT DF_ta_trs_deposit_crtjam DEFAULT('00:00:00'),
    crtipa VARCHAR(40) NOT NULL CONSTRAINT DF_ta_trs_deposit_crtipa DEFAULT(''),
    crtver VARCHAR(12) NOT NULL CONSTRAINT DF_ta_trs_deposit_crtver DEFAULT(''),
    crtusr VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_deposit_crtusr DEFAULT(''),
    updtgl VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_deposit_updtgl DEFAULT('3000-01-01'),
    updjam VARCHAR(8) NOT NULL CONSTRAINT DF_ta_trs_deposit_updjam DEFAULT('00:00:00'),
    updipa VARCHAR(40) NOT NULL CONSTRAINT DF_ta_trs_deposit_updipa DEFAULT(''),
    updver VARCHAR(12) NOT NULL CONSTRAINT DF_ta_trs_deposit_updver DEFAULT(''),
    updusr VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_deposit_updusr DEFAULT(''),
    fs_provider_vaid VARCHAR(3) NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_provider_vaid DEFAULT(''),

    CONSTRAINT PK_ta_trs_deposit PRIMARY KEY CLUSTERED(fs_kd_trs)
)
GO

CREATE INDEX IX_ta_trs_deposit_reg
    ON ta_trs_deposit (fs_kd_reg, fs_kd_trs)
GO

CREATE INDEX IX_ta_trs_deposit_fs_kd_trs_gen
    ON ta_trs_deposit (fs_kd_trs_gen, fs_kd_trs)
GO
