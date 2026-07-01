CREATE TABLE tb_kelompok
(
    fs_kd_kelompok   VARCHAR(3)  NOT NULL CONSTRAINT DF_tb_kelompok_fs_kd_kelompok DEFAULT(''),
    fs_nm_kelompok   VARCHAR(30) NOT NULL CONSTRAINT DF_tb_kelompok_fs_nm_kelompok DEFAULT(''),
    CONSTRAINT PK_tb_kelompok PRIMARY KEY CLUSTERED (fs_kd_kelompok)
)