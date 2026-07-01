CREATE TABLE tb_generik
(
    fs_kd_generik VARCHAR(15) NOT NULL CONSTRAINT DF_tb_generik_fs_kd_generik DEFAULT(''),
    fs_nm_generik VARCHAR(254) NOT NULL CONSTRAINT DF_tb_generik_fs_nm_generik DEFAULT(''),
    fs_komposisi VARCHAR(254) NOT NULL CONSTRAINT DF_tb_generik_fs_komposisi DEFAULT(''),
    CONSTRAINT PK_tb_generik PRIMARY KEY CLUSTERED(fs_kd_generik)
)