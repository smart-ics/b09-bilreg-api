CREATE TABLE ta_grup_jaminan (
    fs_kd_grup_jaminan VARCHAR(3) NOT NULL CONSTRAINT DF_ta_grup_jaminan_fs_kd_grup_jaminan DEFAULT (''),
    fs_nm_grup_jaminan VARCHAR(30) NOT NULL CONSTRAINT DF_ta_grup_jaminan_fs_nm_grup_jaminan DEFAULT (''),
    fb_karyawan BIT NOT NULL CONSTRAINT DF_ta_grup_jaminan_fb_karyawan DEFAULT (0),
    fs_keterangan VARCHAR(35) NOT NULL CONSTRAINT DF_ta_grup_jaminan_fs_keterangan DEFAULT (''),
    
    CONSTRAINT PK_ta_grup_jaminan PRIMARY KEY CLUSTERED (fs_kd_grup_jaminan)
)