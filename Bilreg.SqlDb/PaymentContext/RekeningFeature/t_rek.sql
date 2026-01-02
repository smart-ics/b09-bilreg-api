CREATE TABLE t_rek(
    fs_kd_rek VARCHAR(5) NOT NULL CONSTRAINT DF_t_rek_fs_kd_rek DEFAULT(''),
    fs_nm_rek VARCHAR(5) NOT NULL CONSTRAINT DF_t_rek_fs_nm_rek DEFAULT('')
    CONSTRAINT PK_t_rek PRIMARY KEY CLUSTERED(t_rek)                 
)