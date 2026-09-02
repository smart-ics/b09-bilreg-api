CREATE TABLE tb_trs_rjual_umum2
(
    fs_kd_trs                 VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fs_kd_trs DEFAULT(''),
    fs_kd_trs2                VARCHAR(13) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fs_kd_trs2 DEFAULT(''),
    fn_no_urut                DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_no_urut DEFAULT(0),
    fb_void                   BIT NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fb_void DEFAULT(0),
    fs_kd_barang              VARCHAR(13) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fs_kd_barang DEFAULT(''),
    fs_kd_satuan              VARCHAR(3) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fs_kd_satuan DEFAULT(''),
    fn_qty_jual               DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_qty_jual DEFAULT(0),
    fn_qty_retur              DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_qty_retur DEFAULT(0),
    fn_harga_jual             DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_harga_jual DEFAULT(0),
    fn_harga_retur            DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_harga_retur DEFAULT(0),
    fn_tax                    DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_tax DEFAULT(0),
    fn_sub_total_jual         DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_sub_total_jual DEFAULT(0),
    fn_sub_total_retur        DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_sub_total_retur DEFAULT(0),
    fn_sub_total_tax          DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_sub_total_tax DEFAULT(0),
    fn_total                  DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_fn_total DEFAULT(0),

    CRTTGL                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_CRTTGL DEFAULT('3000-01-01'),
    CRTJAM                    VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_CRTJAM DEFAULT(''),
    CRTIPA                    VARCHAR(40) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_CRTIPA DEFAULT(''),
    CRTVER                    VARCHAR(12) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_CRTVER DEFAULT(''),
    CRTUSR                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_CRTUSR DEFAULT(''),
    UPDTGL                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_UPDTGL DEFAULT('3000-01-01'),
    UPDJAM                    VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_UPDJAM DEFAULT(''),
    UPDIPA                    VARCHAR(40) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_UPDIPA DEFAULT(''),
    UPDVER                    VARCHAR(12) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_UPDVER DEFAULT(''),
    UPDUSR                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_rjual_umum2_UPDUSR DEFAULT(''),

    CONSTRAINT PK_tb_trs_rjual_umum2 PRIMARY KEY CLUSTERED(fs_kd_trs, fs_kd_trs2)
)
