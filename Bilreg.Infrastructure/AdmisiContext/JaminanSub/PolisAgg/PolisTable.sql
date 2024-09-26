CREATE TABLE ta_polis(
    fs_kd_polis VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_fs_kd_polis DEFAULT(''),
    fs_kd_tipe_jaminan VARCHAR(5) NOT NULL CONSTRAINT DF_ta_polis_fs_kd_tipe_jaminan DEFAULT(''),
    fs_no_polis VARCHAR(30) NOT NULL CONSTRAINT DF_ta_polis_fs_no_polis DEFAULT(''),
    fs_atas_nama VARCHAR(40) NOT NULL CONSTRAINT DF_ta_polis_fs_atas_nama DEFAULT(''),
    fd_expired VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_fd_expired DEFAULT(''),
    fs_kd_kelas_ri VARCHAR(3) NOT NULL CONSTRAINT DF_ta_polis_fs_kd_kelas_ri DEFAULT(''),
    fb_cover_rj BIT NOT NULL CONSTRAINT DF_ta_polis_fb_cover_rj DEFAULT(''),
    
    CONSTRAINT PK_ta_polis PRIMARY KEY(fs_kd_polis)
)
