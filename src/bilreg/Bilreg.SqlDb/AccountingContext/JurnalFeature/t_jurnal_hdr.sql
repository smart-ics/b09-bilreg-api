CREATE TABLE t_jurnal_hdr
(
    fs_kd_jurnal VARCHAR(26) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_kd_jurnal DEFAULT(''),
    fd_tgl_jurnal VARCHAR(10) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fd_tgl_jurnal DEFAULT('3000-01-01'),
    fs_jam_jurnal VARCHAR(8) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_jam_jurnal DEFAULT('00:00:00'),
    fs_kd_petugas VARCHAR(10) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_kd_petugas DEFAULT(''),

    fs_keterangan VARCHAR(250) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_keterangan DEFAULT(''),
    fs_no_bukti1 VARCHAR(30) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_no_bukti1 DEFAULT(''),
    fs_no_bukti2 VARCHAR(30) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_no_bukti2 DEFAULT(''),
    fs_no_bukti3 VARCHAR(30) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_no_bukti3 DEFAULT(''),

    fs_kd_reg VARCHAR(10) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_kd_reg DEFAULT(''),
    fs_kd_mr VARCHAR(15) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_kd_mr DEFAULT(''),

    CONSTRAINT PK_t_jurnal_hdr PRIMARY KEY CLUSTERED(fs_kd_jurnal)
)
