CREATE TABLE tb_stok
(
    fs_kd_trs VARCHAR(10) NOT NULL CONSTRAINT DF_tb_stok_fs_kd_trs DEFAULT(''),
    fs_kd_barang VARCHAR(13) NOT NULL CONSTRAINT DF_tb_stok_fs_kd_barang DEFAULT(''),
    fs_kd_layanan VARCHAR(5) NOT NULL CONSTRAINT DF_tb_stok_fs_kd_layanan DEFAULT(''),
    
    fs_kd_po VARCHAR(10) NOT NULL CONSTRAINT DF_tb_stok_fs_kd_po DEFAULT(''),
    fs_kd_do VARCHAR(10) NOT NULL CONSTRAINT DF_tb_stok_fs_kd_do DEFAULT(''),
    fd_tgl_ed VARCHAR(10) NOT NULL CONSTRAINT DF_tb_stok_fd_tgl_ed DEFAULT('3000-01-01'),
    fs_no_batch VARCHAR(15) NOT NULL CONSTRAINT DF_tb_stok_fs_no_batch DEFAULT(''),

    fn_qty DECIMAL(18,0) NOT NULL CONSTRAINT DF_tb_stok_fn_qty DEFAULT(0),
    fn_qty_in DECIMAL(18,0) NOT NULL CONSTRAINT DF_tb_stok_fn_qty_in DEFAULT(0),
    fn_hpp DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_stok_fn_hpp DEFAULT(0),

    fd_tgl_do VARCHAR(10) NOT NULL CONSTRAINT DF_tb_stok_fd_tgl_do DEFAULT('3000-01-01'),
    fs_jam_do VARCHAR(8) NOT NULL CONSTRAINT DF_tb_stok_fs_jam_do DEFAULT('00:00:00'),
    fs_kd_mutasi VARCHAR(10) NOT NULL CONSTRAINT DF_tb_stok_fs_kd_mutasi DEFAULT(''),
    fd_tgl_mutasi VARCHAR(10) NOT NULL CONSTRAINT DF_tb_stok_fd_tgl_mutasi DEFAULT('3000-01-01'),
    fs_jam_mutasi VARCHAR(8) NOT NULL CONSTRAINT DF_tb_stok_fs_jam_mutasi DEFAULT('00:00:00'),

    fs_kd_satuan VARCHAR(3) NOT NULL CONSTRAINT DF_tb_stok_fs_kd_satuan DEFAULT(''),
    
    CONSTRAINT PK_tb_stok PRIMARY KEY CLUSTERED (fs_kd_trs)
)