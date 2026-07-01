CREATE TABLE t_unit
(
    fs_kd_unit VARCHAR(10) NOT NULL CONSTRAINT DF_t_unit_fs_kd_unit DEFAULT(''),
    fs_nm_unit VARCHAR(30) NOT NULL CONSTRAINT DF_t_unit_fs_nm_unit DEFAULT(''),
    fn_urut INT NOT NULL CONSTRAINT DF_t_unit_fn_urut DEFAULT(0),
    fb_rugi_laba BIT NOT NULL CONSTRAINT DF_t_unit_fb_rugi_laba DEFAULT(0),
    fs_kd_unit_grup VARCHAR(5) NOT NULL CONSTRAINT DF_t_unit_fs_kd_unit_grup DEFAULT(''),

    CONSTRAINT PK_t_unit PRIMARY KEY CLUSTERED(fs_kd_unit)
)