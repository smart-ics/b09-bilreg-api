CREATE TABLE tb_golongan
(
    fs_kd_golongan   VARCHAR(3)  NOT NULL CONSTRAINT DF_tb_golongan_fs_kd_golongan DEFAULT(''),
    fs_nm_golongan   VARCHAR(30) NOT NULL CONSTRAINT DF_tb_golongan_fs_nm_golongan DEFAULT(''),
    CONSTRAINT PK_tb_golongan PRIMARY KEY CLUSTERED (fs_kd_golongan)
)