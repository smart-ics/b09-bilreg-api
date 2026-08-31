CREATE TABLE ta_trs_roomcharge
(
    fs_kd_trs          VARCHAR(15)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_kd_trs DEFAULT(''),
    fd_tgl_trs         VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fd_tgl_trs DEFAULT('3000-01-01'),
    fs_jam_trs         VARCHAR(8)    NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_jam_trs DEFAULT('00:00:00'),
    fs_kd_petugas      VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_kd_petugas DEFAULT(''),
    fs_kd_reg          VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_kd_reg DEFAULT(''),
    fs_kd_layanan      VARCHAR(5)    NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_kd_layanan DEFAULT(''),
    fs_kd_bed          VARCHAR(8)    NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_kd_bed DEFAULT(''),
    fn_tarif           DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fn_tarif DEFAULT(0),
    fn_diskon          DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fn_diskon DEFAULT(0),
    fn_total           DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fn_total DEFAULT(0),
    fs_ket             VARCHAR(40)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_ket DEFAULT(''),
    fn_qty             DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fn_qty DEFAULT(0),
    fb_gen_nm_kelas    BIT           NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fb_gen_nm_kelas DEFAULT(1),
    fs_kd_tipe_jaminan VARCHAR(5)    NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_kd_tipe_jaminan DEFAULT(''),
    fn_nilai_klaim     DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fn_nilai_klaim DEFAULT(0),
    fs_kd_trs_dx       VARCHAR(10)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_kd_trs_dx DEFAULT(''),
    fs_kd_pakai_bed    VARCHAR(12)   NOT NULL CONSTRAINT DF_ta_trs_roomcharge_fs_kd_pakai_bed DEFAULT(''),

    CONSTRAINT PK_ta_trs_roomcharge PRIMARY KEY CLUSTERED(fs_kd_trs)
)