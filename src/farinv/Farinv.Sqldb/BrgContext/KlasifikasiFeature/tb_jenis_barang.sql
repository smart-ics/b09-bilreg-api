CREATE TABLE tb_jenis_barang
(
    fs_kd_jenis_barang   VARCHAR(2)  NOT NULL CONSTRAINT DF_tb_jenis_barang_fs_kd_jenis_barang DEFAULT(''),
    fs_nm_jenis_barang   VARCHAR(15) NOT NULL CONSTRAINT DF_tb_jenis_barang_fs_nm_jenis_barang DEFAULT(''),
    CONSTRAINT PK_tb_jenis_barang PRIMARY KEY CLUSTERED (fs_kd_jenis_barang)
)