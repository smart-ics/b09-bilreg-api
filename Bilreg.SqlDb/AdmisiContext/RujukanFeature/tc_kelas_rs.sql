CREATE TABLE tc_kelas_rs (
    fs_kd_kelas_rs VARCHAR(1) NOT NULL CONSTRAINT DF_tc_kelas_rs_fs_kd_kelas_rs DEFAULT(''),
    fs_nm_kelas_rs VARCHAR(20) NOT NULL CONSTRAINT DF_tc_kelas_rs_fs_nm_kelas_rs DEFAULT(''),
    fn_nilai DECIMAL(18,2) NOT NULL CONSTRAINT DF_tc_kelas_rs_fn_nilai DEFAULT(0),

    CONSTRAINT PK_tc_kelas_rs PRIMARY KEY CLUSTERED (fs_kd_kelas_rs)
);