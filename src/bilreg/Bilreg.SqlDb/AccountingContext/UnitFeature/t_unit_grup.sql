CREATE TABLE t_unit_grup
(
    fs_kd_unit_grup VARCHAR(5) NOT NULL CONSTRAINT DF_t_unit_grup_fs_kd_unit_grup DEFAULT(''),
    fs_nm_unit_grup VARCHAR(30) NOT NULL CONSTRAINT DF_t_unit_grup_fs_nm_unit_grup DEFAULT(''),
    fn_urut INT NOT NULL CONSTRAINT DF_t_unit_grup_fn_urut DEFAULT(0),
    fb_rugi_laba BIT NOT NULL CONSTRAINT DF_t_unit_grup_fb_rugi_laba DEFAULT(0),

    CONSTRAINT PK_t_unit_grup PRIMARY KEY CLUSTERED(fs_kd_unit_grup)
)