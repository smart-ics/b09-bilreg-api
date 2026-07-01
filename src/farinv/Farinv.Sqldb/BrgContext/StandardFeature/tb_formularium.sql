CREATE TABLE tb_formularium (
    fs_kd_formularium VARCHAR(5) NOT NULL CONSTRAINT DF_tb_formularium_fs_kd_formularium DEFAULT(''),
    fs_nm_formularium VARCHAR(30) NOT NULL CONSTRAINT DF_tb_formularium_fs_nm_formularium DEFAULT(''),
    
    CONSTRAINT PK_tb_formularium PRIMARY KEY CLUSTERED(fs_kd_formularium)
)