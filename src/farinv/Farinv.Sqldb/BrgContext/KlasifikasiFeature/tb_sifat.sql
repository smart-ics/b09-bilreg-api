CREATE TABLE tb_sifat
(
    fs_kd_sifat   VARCHAR(3)  NOT NULL CONSTRAINT DF_tb_sifat_fs_kd_sifat DEFAULT(''),
    fs_nm_sifat   VARCHAR(30) NOT NULL CONSTRAINT DF_tb_sifat_fs_nm_sifat DEFAULT(''),
    CONSTRAINT PK_tb_sifat PRIMARY KEY CLUSTERED (fs_kd_sifat)
)