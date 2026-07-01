CREATE TABLE tb_barang_satuan
(
    fs_kd_barang VARCHAR(13) NOT NULL CONSTRAINT DF_tb_barang_satuan_fs_kd_barang DEFAULT(''),
    fs_kd_satuan VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_satuan_fs_kd_satuan DEFAULT(''),
    fn_nilai DECIMAL(18,0) NOT NULL CONSTRAINT DF_tb_barang_satuan_fn_nilai DEFAULT(0),
    CONSTRAINT PK_tb_barang_satuan PRIMARY KEY CLUSTERED(fs_kd_barang, fs_kd_satuan)
)
