CREATE TABLE ta_polis_cover(
    fs_kd_polis VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_fs_kd_polis DEFAULT(''),
    fs_mr VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_fs_mr DEFAULT(''),
    fs_kd_status VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_fs_kd_status DEFAULT(''),
    fd_tgl_expired VARCHAR(10) NOT NULL CONSTRAINT DF_ta_polis_cover_fd_tgl_expired DEFAULT(''),
)
GO

CREATE CLUSTERED INDEX IX_ta_polis_cover_fs_kd_polis 
    ON ta_polis_cover(fs_kd_polis)
GO
