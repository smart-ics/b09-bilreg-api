CREATE TABLE tb_trs_dobill_umum
(
    fs_kd_trs                 VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_trs DEFAULT(''),

    fd_tgl_trs                VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fd_tgl_trs DEFAULT(''),
    fs_jam_trs                VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_jam_trs DEFAULT(''),
    fs_kd_petugas             VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_petugas DEFAULT(''),
    fd_tgl_void               VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fd_tgl_void DEFAULT(''),
    fs_jam_void               VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_jam_void DEFAULT(''),
    fs_kd_petugas_void        VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_petugas_void DEFAULT(''),

    fs_kd_resep               VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_resep DEFAULT(''),
    fs_kd_layanan_resep       VARCHAR(5) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_layanan_resep DEFAULT(''),
    fs_kd_petugas_medis       VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_petugas_medis DEFAULT(''),
    fs_kd_layanan             VARCHAR(5) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_layanan DEFAULT(''),

    fs_kd_tipe_jaminan        VARCHAR(5) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_tipe_jaminan DEFAULT(''),
    fs_kd_tipe_barang         VARCHAR(2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_tipe_barang DEFAULT(''),

    fs_kd_reg                 VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kd_reg DEFAULT(''),
    fs_nm_pasien              VARCHAR(40) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_nm_pasien DEFAULT(''),
    fs_alm_pasien             VARCHAR(40) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_alm_pasien DEFAULT(''),
    fs_alm2_pasien            VARCHAR(40) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_alm2_pasien DEFAULT(''),
    fs_kota_pasien            VARCHAR(20) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_kota_pasien DEFAULT(''),
    fs_sex                    VARCHAR(1) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fs_sex DEFAULT(''),

    fn_sum_sub_total          DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fn_sum_sub_total DEFAULT(0),
    fn_sum_biaya              DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fn_sum_biaya DEFAULT(0),
    fn_sum_tax_rupiah         DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fn_sum_tax_rupiah DEFAULT(0),
    fn_sub_total              DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fn_sub_total DEFAULT(0),
    fn_diskon_lain            DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fn_diskon_lain DEFAULT(0),
    fn_biaya_lain             DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fn_biaya_lain DEFAULT(0),
    fn_grand_total            DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fn_grand_total DEFAULT(0),
    fn_pembulatan             DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fn_pembulatan DEFAULT(0),
    fn_bulat                  DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_fn_bulat DEFAULT(0),

    CRTTGL                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_CRTTGL DEFAULT(''),
    CRTJAM                    VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_CRTJAM DEFAULT(''),
    CRTIPA                    VARCHAR(40) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_CRTIPA DEFAULT(''),
    CRTVER                    VARCHAR(12) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_CRTVER DEFAULT(''),
    CRTUSR                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_CRTUSR DEFAULT(''),
    UPDTGL                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_UPDTGL DEFAULT(''),
    UPDJAM                    VARCHAR(8) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_UPDJAM DEFAULT(''),
    UPDIPA                    VARCHAR(40) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_UPDIPA DEFAULT(''),
    UPDVER                    VARCHAR(12) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_UPDVER DEFAULT(''),
    UPDUSR                    VARCHAR(10) NOT NULL CONSTRAINT DF_tb_trs_dobill_umum_UPDUSR DEFAULT(''),

    CONSTRAINT PK_tb_trs_dobill_umum PRIMARY KEY CLUSTERED(fs_kd_trs)
)