CREATE TABLE ta_tipe_barang
(
    fs_kd_tipe_barang   VARCHAR(2)  NOT NULL CONSTRAINT DF_ta_tipe_barang_fs_kd_tipe_barang DEFAULT(''),
    fs_nm_tipe_barang   VARCHAR(20) NOT NULL CONSTRAINT DF_ta_tipe_barang_fs_nm_tipe_barang DEFAULT(''),
    fb_aktif            BIT NOT NULL CONSTRAINT DF_ta_tipe_barang_fb_aktif DEFAULT(0),
    fn_biaya_per_barang DECIMAL(18,0) NOT NULL CONSTRAINT DF_ta_tipe_barang_fn_biaya_per_barang DEFAULT(0),
    fn_biaya_per_racik  DECIMAL(18,0) NOT NULL CONSTRAINT DF_ta_tipe_barang_fn_biaya_per_racik DEFAULT(0),
    fn_profit           DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_tipe_barang_fn_profit DEFAULT(0),
    fn_tax              DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_tipe_barang_fn_tax DEFAULT(0),
    fn_diskon           DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_tipe_barang_fn_diskon DEFAULT(0),
    CONSTRAINT PK_ta_tipe_barang PRIMARY KEY CLUSTERED (fs_kd_tipe_barang)
)