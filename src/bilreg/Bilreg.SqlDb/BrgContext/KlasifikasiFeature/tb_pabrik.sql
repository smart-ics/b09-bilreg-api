CREATE TABLE tb_pabrik
(
    fs_kd_pabrik      VARCHAR(5)  NOT NULL CONSTRAINT DF_tb_pabrik_fs_kd_pabrik DEFAULT(''),
    fs_nm_pabrik      VARCHAR(60) NOT NULL CONSTRAINT DF_tb_pabrik_fs_nm_pabrik DEFAULT(''),
    fn_koef_formula   DECIMAL(18,0) NOT NULL CONSTRAINT DF_tb_pabrik_fn_koef_formula DEFAULT(0),
    CONSTRAINT PK_tb_pabrik PRIMARY KEY CLUSTERED (fs_kd_pabrik)
)