CREATE TABLE ta_polis(
    fs_kd_polis VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_fs_kd_polis DEFAULT(''),
    fs_kd_tipe_jaminan VARCHAR(5) NOT NULL CONSTRAINT DF_ta_polis_fs_kd_tipe_jaminan DEFAULT(''),
    fs_no_polis VARCHAR(30) NOT NULL CONSTRAINT DF_ta_polis_fs_no_polis DEFAULT(''),
    fs_atas_nama VARCHAR(40) NOT NULL CONSTRAINT DF_ta_polis_fs_atas_nama DEFAULT(''),
    fd_expired VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_fd_expired DEFAULT('3000-01-01'),
    fs_kd_kelas_ri VARCHAR(3) NOT NULL CONSTRAINT DF_ta_polis_fs_kd_kelas_ri DEFAULT(''),
    fb_cover_rj BIT NOT NULL CONSTRAINT DF_ta_polis_fb_cover_rj DEFAULT(0),
    fs_kd_bu VARCHAR(8) NOT NULL CONSTRAINT DF_ta_polis_fs_kd_bu DEFAULT(''),
    fs_departemen VARCHAR(40) NOT NULL CONSTRAINT DF_ta_polis_fs_departemen DEFAULT(''),
    fs_no_payroll VARCHAR(40) NOT NULL CONSTRAINT DF_ta_polis_fs_no_payroll DEFAULT(''),
    crttgl VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_crttgl DEFAULT('3000-01-01'),
    crtjam VARCHAR(8) NOT NULL CONSTRAINT DF_ta_polis_crtjam DEFAULT('00:00:00'),
    crtipa VARCHAR(40) NOT NULL CONSTRAINT DF_ta_polis_crtipa DEFAULT(''),
    crtver VARCHAR(12) NOT NULL CONSTRAINT DF_ta_polis_crtver DEFAULT(''),
    crtusr VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_crtusr DEFAULT(''),
    updtgl VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_updtgl DEFAULT('3000-01-01'),
    updjam VARCHAR(8) NOT NULL CONSTRAINT DF_ta_polis_updjam DEFAULT('00:00:00'),
    updipa VARCHAR(40) NOT NULL CONSTRAINT DF_ta_polis_updipa DEFAULT(''),
    updver VARCHAR(12) NOT NULL CONSTRAINT DF_ta_polis_updver DEFAULT(''),
    updusr VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_updusr DEFAULT(''),
    
    CONSTRAINT PK_ta_polis PRIMARY KEY(fs_kd_polis)
)
