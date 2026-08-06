CREATE TABLE ta_trs_kartu_periksa3
(
    fs_kd_trs          VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_kd_trs DEFAULT(''),
    fn_no_urut         INT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_no_urut DEFAULT(0),
    fs_kd_barang       VARCHAR(15) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_kd_barang DEFAULT(''),
    fs_nm_barang       VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_nm_barang DEFAULT(''),
    fn_qty_barang      DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_qty_barang DEFAULT(0),
    fs_kd_satuan       VARCHAR(5) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_kd_satuan DEFAULT(''),
    fn_iter_barang     INT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_iter_barang DEFAULT(0),
    fb_racik           BIT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fb_racik DEFAULT(0),
    fb_komponen        BIT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fb_komponen DEFAULT(0),
    fs_kd_racik        VARCHAR(15) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_kd_racik DEFAULT(''),
    fn_qty_racik       DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_qty_racik DEFAULT(0),
    fs_racik_qty       VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_racik_qty DEFAULT(''),
    fs_keterangan      VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_keterangan DEFAULT(''),
    fs_etiket_desc     VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_etiket_desc DEFAULT(''),
    fn_etiket_qty      INT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_etiket_qty DEFAULT(0),
    fn_etiket_hari     DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_etiket_hari DEFAULT(0),

    CONSTRAINT PK_ta_trs_kartu_periksa3 PRIMARY KEY CLUSTERED(fs_kd_trs, fn_no_urut)
)
