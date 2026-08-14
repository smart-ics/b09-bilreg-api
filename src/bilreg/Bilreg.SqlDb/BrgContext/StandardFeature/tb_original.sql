CREATE TABLE tb_original
(
    fs_kd_original   VARCHAR(3)  NOT NULL CONSTRAINT DF_tb_original_fs_kd_original DEFAULT(''),
    fs_nm_original   VARCHAR(30) NOT NULL CONSTRAINT DF_tb_original_fs_nm_original DEFAULT(''),
    CONSTRAINT PK_tb_original PRIMARY KEY CLUSTERED (fs_kd_original)
)