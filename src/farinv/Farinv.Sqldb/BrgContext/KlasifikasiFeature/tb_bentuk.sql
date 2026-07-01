CREATE TABLE tb_bentuk
(
    fs_kd_bentuk   VARCHAR(3)  NOT NULL CONSTRAINT DF_tb_bentuk_fs_kd_bentuk DEFAULT(''),
    fs_nm_bentuk   VARCHAR(30) NOT NULL CONSTRAINT DF_tb_bentuk_fs_nm_bentuk DEFAULT(''),
    CONSTRAINT PK_tb_bentuk PRIMARY KEY CLUSTERED (fs_kd_bentuk)
)