CREATE TABLE tb_gol_obat_dk
(
    fs_kd_gol_obat_dk   VARCHAR(3)  NOT NULL CONSTRAINT DF_tb_gol_obat_dk_fs_kd_gol_obat_dk DEFAULT(''),
    fs_nm_gol_obat_dk   VARCHAR(30) NOT NULL CONSTRAINT DF_tb_gol_obat_dk_fs_nm_gol_obat_dk DEFAULT(''),
    CONSTRAINT PK_tb_gol_obat_dk PRIMARY KEY CLUSTERED (fs_kd_gol_obat_dk)
)