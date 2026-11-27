CREATE TABLE ta_polis_cover(
    fs_kd_polis VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_fs_kd_polis DEFAULT(''),
    fs_mr VARCHAR(15) NOT NULL CONSTRAINT DF_ta_polis_cover_fs_mr DEFAULT(''),
    fs_kd_status VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_fs_kd_status DEFAULT(''),
    fd_tgl_expired VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_fd_tgl_expired DEFAULT('3000-01-01'),
    crttgl VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_crttgl DEFAULT('3000-01-01'),
    crtjam VARCHAR(8) NOT NULL CONSTRAINT DF_ta_polis_cover_crtjam DEFAULT('00:00:00'),
    crtipa VARCHAR(40) NOT NULL CONSTRAINT DF_ta_polis_cover_crtipa DEFAULT(''),
    crtver VARCHAR(12) NOT NULL CONSTRAINT DF_ta_polis_cover_crtver DEFAULT(''),
    crtusr VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_crtusr DEFAULT(''),
    updtgl VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_updtgl DEFAULT('3000-01-01'),
    updjam VARCHAR(8) NOT NULL CONSTRAINT DF_ta_polis_cover_updjam DEFAULT('00:00:00'),
    updipa VARCHAR(40) NOT NULL CONSTRAINT DF_ta_polis_cover_updipa DEFAULT(''),
    updver VARCHAR(12) NOT NULL CONSTRAINT DF_ta_polis_cover_updver DEFAULT(''),
    updusr VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_updusr DEFAULT(''),
)
GO

CREATE CLUSTERED INDEX IX_ta_polis_cover_fs_kd_polis 
    ON ta_polis_cover(fs_kd_polis)
GO
