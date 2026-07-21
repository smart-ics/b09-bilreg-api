CREATE TABLE ta_registrasi(
    fs_kd_reg VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_reg DEFAULT(''),
    fd_tgl_masuk VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fd_tgl_masuk DEFAULT('3000-01-01'),
    fs_jam_masuk VARCHAR (8) NOT NULL CONSTRAINT DF_ta_registrasi_fs_jam_masuk DEFAULT('00:00:00'),
    fs_kd_petugas VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_petugas DEFAULT(''),
    fd_tgl_keluar VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fd_tgl_keluar DEFAULT('3000-01-01'),
    fs_jam_keluar VARCHAR (8) NOT NULL CONSTRAINT DF_ta_registrasi_fs_jam_keluar DEFAULT('00:00:00'),
    fs_kd_petugas_keluar VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_petugas_keluar DEFAULT(''),
    fd_tgl_cancel_out VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fd_tgl_cancel_out DEFAULT('3000-01-01'),
    fs_jam_cancel_out VARCHAR (8) NOT NULL CONSTRAINT DF_ta_registrasi_fs_jam_cancel_out DEFAULT('00:00:00'),
    fs_kd_petugas_cancel_out VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_petugas_cancel_out DEFAULT(''),
    fs_kd_jenis_reg VARCHAR (2) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_jenis_reg DEFAULT(''),
    fs_mr VARCHAR (15) NOT NULL CONSTRAINT DF_ta_registrasi_fs_mr DEFAULT(''),
    fs_kd_tipe_jaminan VARCHAR (5) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_tipe_jaminan DEFAULT(''),
    fs_kd_kelas VARCHAR (3) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_kelas DEFAULT(''),
    fs_kd_cara_masuk_dk VARCHAR (1) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_cara_masuk_dk DEFAULT(''),
    fs_kd_rujukan VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_rujukan DEFAULT(''),
    fs_kd_medis VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_medis DEFAULT(''),
    fs_kd_layanan VARCHAR (5) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_layanan DEFAULT(''),
    fs_kd_karcis VARCHAR (2) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_karcis DEFAULT(''),
    fd_tgl_void VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fd_tgl_void DEFAULT('3000-01-01'),
    fs_jam_void VARCHAR (8) NOT NULL CONSTRAINT DF_ta_registrasi_fs_jam_void DEFAULT('00:00:00'),
    fs_kd_petugas_void VARCHAR (10) NOT NULL CONSTRAINT DF_ta_registrasi_fs_kd_petugas_void DEFAULT(''),
    fn_karcis decimal (18, 2) NOT NULL CONSTRAINT DF_ta_registrasi_fn_karcis DEFAULT(0),
    fn_karcis_sisa decimal (18, 2) NOT NULL CONSTRAINT DF_ta_registrasi_fn_karcis_sisa DEFAULT(0),
    
    CONSTRAINT PK_ta_registrasi PRIMARY KEY CLUSTERED (fs_kd_reg)
)
GO

CREATE INDEX IX_ta_registrasi_tgl_masuk
    ON ta_registrasi (fd_tgl_masuk, fs_kd_reg)
GO

