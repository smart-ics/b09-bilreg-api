CREATE TABLE tb_trs_rjual_umum
(
    fs_kd_trs                 VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_kd_trs DEFAULT(''),

    fd_tgl_trs                VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fd_tgl_trs DEFAULT('3000-01-01'),
    fs_jam_trs                VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_jam_trs DEFAULT(''),
    fs_kd_petugas             VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_kd_petugas DEFAULT(''),
    fd_tgl_void               VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fd_tgl_void DEFAULT('3000-01-01'),
    fs_jam_void               VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_jam_void DEFAULT(''),
    fs_kd_petugas_void        VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_kd_petugas_void DEFAULT(''),

    fs_kd_dobill_umum         VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_kd_dobill_umum DEFAULT(''),
    fs_kd_reg                 VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_kd_reg DEFAULT(''),
    fs_kd_layanan             VARCHAR(5) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_kd_layanan DEFAULT(''),
    fs_keterangan             VARCHAR(30) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_keterangan DEFAULT(''),
    fs_kd_tipe_jaminan        VARCHAR(5) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_kd_tipe_jaminan DEFAULT(''),
    fs_kd_tipe_barang         VARCHAR(2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fs_kd_tipe_barang DEFAULT(''),

    fn_total_jual             DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fn_total_jual DEFAULT(0),
    fn_total_retur            DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fn_total_retur DEFAULT(0),
    fn_total_tax              DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fn_total_tax DEFAULT(0),
    fn_pembulatan             DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fn_pembulatan DEFAULT(0),
    fn_grand_total            DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_fn_grand_total DEFAULT(0),

    CRTTGL                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_CRTTGL DEFAULT('3000-01-01'),
    CRTJAM                    VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_CRTJAM DEFAULT(''),
    CRTIPA                    VARCHAR(40) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_CRTIPA DEFAULT(''),
    CRTVER                    VARCHAR(12) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_CRTVER DEFAULT(''),
    CRTUSR                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_CRTUSR DEFAULT(''),
    UPDTGL                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_UPDTGL DEFAULT('3000-01-01'),
    UPDJAM                    VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_UPDJAM DEFAULT(''),
    UPDIPA                    VARCHAR(40) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_UPDIPA DEFAULT(''),
    UPDVER                    VARCHAR(12) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_UPDVER DEFAULT(''),
    UPDUSR                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum_UPDUSR DEFAULT(''),

    CONSTRAINT PK_tb_trs_rjual_umum PRIMARY KEY CLUSTERED(fs_kd_trs)
)
