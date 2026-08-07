CREATE TABLE tb_grup_rek
(
    fs_kd_grup_rek VARCHAR(5) NOT NULL CONSTRAINT DF_tb_grup_rek_fs_kd_grup_rek DEFAULT(''),
    fs_nm_grup_rek VARCHAR(30) NOT NULL CONSTRAINT DF_tb_grup_rek_fs_nm_grup_rek DEFAULT(''),
    fs_kd_grup_rek_dk VARCHAR(3) NOT NULL CONSTRAINT DF_tb_grup_rek_fs_kd_grup_rek_dk DEFAULT(''),
    CONSTRAINT PK_tb_grup_rek PRIMARY KEY CLUSTERED(fs_kd_grup_rek)
)