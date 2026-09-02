CREATE TABLE ta_trs_deposit
(
    fs_kd_trs               VARCHAR(12)   NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs DEFAULT(''),
    fd_tgl_trs              VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_deposit_fd_tgl_trs DEFAULT('3000-01-01'),
    fs_jam_trs              VARCHAR(8)    NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_jam_trs DEFAULT('00:00:00'),
    fd_tgl_jam_trs          DATETIME      NOT NULL CONSTRAINT DF_ta_trs_deposit_fd_tgl_jam_trs DEFAULT('3000-01-01'),
    fs_kd_petugas           VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_petugas DEFAULT(''),
    fs_kd_reg               VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_reg DEFAULT(''),
    fs_kd_layanan           VARCHAR(5)    NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_layanan DEFAULT(''),
    fs_keterangan           VARCHAR(255)  NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_keterangan DEFAULT(''),
    fd_tgl_void             VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_deposit_fd_tgl_void DEFAULT('3000-01-01'),
    fs_jam_void             VARCHAR(8)    NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_jam_void DEFAULT('00:00:00'),
    fs_kd_petugas_void      VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_petugas_void DEFAULT(''),
    fs_kd_trs_gen           VARCHAR(12)   NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs_gen DEFAULT(''),
    fs_kd_trs_deposit_khusus VARCHAR(12)  NOT NULL CONSTRAINT DF_ta_trs_deposit_fs_kd_trs_deposit_khusus DEFAULT(''),
    fn_nilai_deposit        DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_nilai_deposit DEFAULT(0),
    fn_total_deposit        DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_deposit_fn_total_deposit DEFAULT(0),
    crttgl                  VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_deposit_crttgl DEFAULT('3000-01-01'),
    crtjam                  VARCHAR(8)    NOT NULL CONSTRAINT DF_ta_trs_deposit_crtjam DEFAULT('00:00:00'),
    crtusr                  VARCHAR(50)   NOT NULL CONSTRAINT DF_ta_trs_deposit_crtusr DEFAULT(''),
    updtgl                  VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_deposit_updtgl DEFAULT('3000-01-01'),
    updjam                  VARCHAR(8)    NOT NULL CONSTRAINT DF_ta_trs_deposit_updjam DEFAULT('00:00:00'),
    updusr                  VARCHAR(50)   NOT NULL CONSTRAINT DF_ta_trs_deposit_updusr DEFAULT(''),

    CONSTRAINT PK_ta_trs_deposit PRIMARY KEY CLUSTERED(fs_kd_trs)
)
GO

CREATE INDEX IX_ta_trs_deposit_reg
    ON ta_trs_deposit (fs_kd_reg)
GO