CREATE TABLE td_peg_sat_tugas(
    fs_kd_peg VARCHAR(10) NOT NULL CONSTRAINT DF_td_peg_sat_tugas_fs_kd_peg DEFAULT(''),
    fs_kd_sat_tugas VARCHAR(5) NOT NULL CONSTRAINT DF_td_peg_sat_tugas_fs_kd_sat_tugas DEFAULT(''),
    fn_utama DECIMAL(18,0) NOT NULL CONSTRAINT DF_td_peg_sat_tugas_fn_utama DEFAULT(0),
 
    CONSTRAINT PK_td_peg_sat_tugas PRIMARY KEY CLUSTERED(fs_kd_peg, fs_kd_sat_tugas)
)
GO

CREATE TABLE td_sat_tugas(
    fs_kd_sat_tugas VARCHAR(2) NOT NULL CONSTRAINT DF_td_sat_tugas_fs_kd_sat_tugas DEFAULT(''),
    fs_nm_sat_tugas VARCHAR(30) NOT NULL CONSTRAINT DF_td_sat_tugas_fs_nm_sat_tugas DEFAULT(''),
    
    CONSTRAINT PK_td_sat_tugas PRIMARY KEY CLUSTERED(fs_kd_sat_tugas)
)