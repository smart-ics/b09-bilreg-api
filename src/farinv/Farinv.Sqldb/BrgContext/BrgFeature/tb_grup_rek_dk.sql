CREATE TABLE tb_grup_rek_dk
(
    fs_kd_grup_rek_dk VARCHAR(3) NOT NULL CONSTRAINT DF_tb_grup_rek_dk_fs_kd_grup_rek_dk DEFAULT(''),
    fs_nm_grup_rek_dk VARCHAR(25) NOT NULL CONSTRAINT DF_tb_grup_rek_dk_fs_nm_grup_rek_dk DEFAULT(''),
    CONSTRAINT PK_tb_grup_rek_dk PRIMARY KEY CLUSTERED(fs_kd_grup_rek_dk)
)