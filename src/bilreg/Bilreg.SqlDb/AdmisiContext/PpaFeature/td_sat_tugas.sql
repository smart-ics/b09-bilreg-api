CREATE TABLE td_sat_tugas (
    fs_kd_sat_tugas VARCHAR(2) NOT NULL CONSTRAINT DF_td_sat_tugas_fs_kd_sat_tugas DEFAULT(''),
    fs_nm_sat_tugas VARCHAR(30) NOT NULL CONSTRAINT DF_td_sat_tugas_fs_nm_sat_tugas DEFAULT(''),
    fb_sat_medis BIT NOT NULL CONSTRAINT DF_td_sat_tugas_fb_sat_medis DEFAULT(0),
    fs_kd_profesi VARCHAR(5) NOT NULL CONSTRAINT DF_td_peg_sat_tugas_fs_kd_profesi DEFAULT(''),
    
    CONSTRAINT PK_td_sat_tugas PRIMARY KEY CLUSTERED (fs_kd_sat_tugas)
);
