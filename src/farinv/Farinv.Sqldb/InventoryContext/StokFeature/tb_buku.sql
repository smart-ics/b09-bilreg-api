CREATE TABLE tb_buku
(
    fs_kd_trs VARCHAR(10) NOT NULL CONSTRAINT DF_tb_buku_fs_kd_trs DEFAULT(''),
    fs_kd_barang VARCHAR(13) NOT NULL CONSTRAINT DF_tb_buku_fs_kd_barang DEFAULT(''),
    fs_kd_layanan VARCHAR(5) NOT NULL CONSTRAINT DF_tb_buku_fs_kd_layanan DEFAULT(''),

    fs_kd_po VARCHAR(10) NOT NULL CONSTRAINT DF_tb_buku_fs_kd_po DEFAULT(''),
    fs_kd_do VARCHAR(10) NOT NULL CONSTRAINT DF_tb_buku_fs_kd_do DEFAULT(''),
    fd_tgl_ed VARCHAR(10) NOT NULL CONSTRAINT DF_tb_buku_fd_tgl_ed DEFAULT('3000-01-01'),
    fs_no_batch VARCHAR(15) NOT NULL CONSTRAINT DF_tb_buku_fs_no_batch DEFAULT(''),

    fn_stok_in DECIMAL(18,0) NOT NULL CONSTRAINT DF_tb_buku_fn_stok_in DEFAULT(0),
    fn_stok_out DECIMAL(18,0) NOT NULL CONSTRAINT DF_tb_buku_fn_stok_out DEFAULT(0),
    fn_hpp DECIMAL(18,2) NOT NULL CONSTRAINT DF_tb_buku_fn_hpp DEFAULT(0),

    fs_kd_mutasi VARCHAR(10) NOT NULL CONSTRAINT DF_tb_buku_fs_kd_mutasi DEFAULT(''),
    fd_tgl_jam_mutasi VARCHAR(19) NOT NULL CONSTRAINT DF_tb_buku_fd_tgl_jam_mutasi DEFAULT('3000-01-01 00:00:00'),
    fd_tgl_mutasi VARCHAR(10) NOT NULL CONSTRAINT DF_tb_buku_fd_tgl_mutasi DEFAULT('3000-01-01'),
    fs_jam_mutasi VARCHAR(8) NOT NULL CONSTRAINT DF_tb_buku_fs_jam_mutasi DEFAULT('00:00:00'),
    fs_kd_jenis_mutasi VARCHAR(10) NOT NULL CONSTRAINT DF_tb_buku_fs_kd_jenis_mutasi DEFAULT(''),

    fs_kd_satuan VARCHAR(3) NOT NULL CONSTRAINT DF_tb_buku_fs_kd_satuan DEFAULT(''),

    CONSTRAINT PK_tb_buku PRIMARY KEY CLUSTERED (fs_kd_trs)
)