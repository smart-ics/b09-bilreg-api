CREATE TABLE ta_trs_roomcharge_log
(
    fs_kd_trs          VARCHAR(15)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fs_kd_trs DEFAULT(' '),
    fd_tgl_trs         VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fd_tgl_trs DEFAULT('3000-01-01'),
    fd_tgl_upd         VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fd_tgl_upd DEFAULT('3000-01-01'),
    fs_jam_upd         VARCHAR(8)    NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fs_jam_upd DEFAULT('00:00:00'),
    fs_kd_petugas      VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fs_kd_petugas DEFAULT(' '),
    fs_kd_upd          VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fs_kd_upd DEFAULT(' '),
    fs_kd_reg          VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fs_kd_reg DEFAULT(' '),
    fs_kd_layanan      VARCHAR(5)    NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fs_kd_layanan DEFAULT(' '),
    fs_kd_bed          VARCHAR(8)    NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fs_kd_bed DEFAULT(' '),
    fn_tarif           DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fn_tarif DEFAULT(0),
    fn_qty             DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fn_qty DEFAULT(0),
    fb_status_void     BIT           NOT NULL CONSTRAINT DF_ta_trs_roomcharge_log_fb_status_void DEFAULT(0),

    CONSTRAINT PK_ta_trs_roomcharge_log PRIMARY KEY CLUSTERED(fs_kd_trs, fd_tgl_trs, fd_tgl_upd, fs_jam_upd)
)