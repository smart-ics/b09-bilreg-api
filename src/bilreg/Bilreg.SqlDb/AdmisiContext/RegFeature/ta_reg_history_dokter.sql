CREATE TABLE ta_reg_history_dokter
(
    fs_kd_reg        VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_history_dokter_fs_kd_reg DEFAULT(''),
    fs_kd_dokter     VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_history_dokter_fs_kd_dokter DEFAULT(''),
    fb_primer        BIT NOT NULL CONSTRAINT DF_ta_reg_history_dokter_fb_primer DEFAULT(0),
    fd_tgl_mulai     VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_history_dokter_fd_tgl_mulai DEFAULT(''),
    fd_tgl_selesai   VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_history_dokter_fd_tgl_selesai DEFAULT(''),
    fb_reg           BIT NOT NULL CONSTRAINT DF_ta_reg_history_dokter_fb_reg DEFAULT(0),

    CONSTRAINT PK_ta_reg_history_dokter PRIMARY KEY CLUSTERED(fs_kd_reg, fs_kd_dokter, fd_tgl_mulai)
)