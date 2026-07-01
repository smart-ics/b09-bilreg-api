CREATE TABLE  tb_barang
(
    fs_kd_barang VARCHAR(13) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_barang DEFAULT(''),
    fs_nm_barang VARCHAR(50) NOT NULL CONSTRAINT DF_tb_barang_fs_nm_barang DEFAULT(''),
    fb_aktif BIT NOT NULL CONSTRAINT DF_tb_barang_fb_aktif DEFAULT(1),
    fs_ket_barang VARCHAR(50) NOT NULL CONSTRAINT DF_tb_barang_fs_ket_barang DEFAULT(''),
    fs_kd_grup_rek VARCHAR(5) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_grup_rek DEFAULT(''),
    fs_kd_golongan VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_golongan DEFAULT(''),
    fs_kd_gol_obat_dk VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_gol_obat_dk DEFAULT(''),
    fs_kd_kelompok VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_kelompok DEFAULT(''),
    fs_kd_sifat VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_sifat DEFAULT(''),
    fs_kd_bentuk VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_bentuk DEFAULT(''),
    fs_kd_pabrik VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_pabrik DEFAULT(''),
    fs_kd_generik VARCHAR(16) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_generik DEFAULT(''),
    fs_kd_gol_terapi VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_gol_terapi DEFAULT(''),
    fs_kd_kelas_terapi VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_kelas_terapi DEFAULT(''),
    fs_kd_original VARCHAR(3) NOT NULL CONSTRAINT DF_tb_barang_fs_kd_original DEFAULT(''),
    
    CONSTRAINT PK_tb_barang PRIMARY KEY CLUSTERED(fs_kd_barang)
)