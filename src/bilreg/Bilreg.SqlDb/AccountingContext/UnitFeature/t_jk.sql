CREATE TABLE t_jk
(
    fs_kd_jk VARCHAR(3) NOT NULL CONSTRAINT DF_t_jk_fs_kd_jk DEFAULT(''),
    fs_nm_jk VARCHAR(30) NOT NULL CONSTRAINT DF_t_jk_fs_nm_jk DEFAULT(''),
    fn_urut INT NOT NULL CONSTRAINT DF_t_jk_fn_urut DEFAULT(0),

    CONSTRAINT PK_t_jk PRIMARY KEY CLUSTERED(fs_kd_jk)
)