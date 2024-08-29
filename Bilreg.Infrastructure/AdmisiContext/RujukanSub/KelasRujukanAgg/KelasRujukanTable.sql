CREATE TABLE tc_kelas_rs (
    fs_kd_kelas VARCHAR(1) NOT NULL CONSTRAINT DF_tc_kelas_rs_fs_kd_kelas DEFAULT(''),
    fs_nm_kelas VARCHAR(20) NOT NULL CONSTRAINT DF_tc_kelas_rs_fs_nm_kelas DEFAULT(''),
    fn_nilai DECIMAL(20, 0) NOT NULL CONSTRAINT DF_tc_kelas_rs_fn_nilai DEFAULT(0),

    CONSTRAINT PK_tc_kelas_rs PRIMARY KEY CLUSTERED (fs_kd_kelas)
);