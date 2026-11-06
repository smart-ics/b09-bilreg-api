CREATE TABLE ta_trs_merge
(
    fs_kd_reg_merge VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_merge_fs_kd_reg_merge DEFAULT(''),
    fs_kd_reg_asal  VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_merge_fs_kd_reg_asal DEFAULT(''),

    fs_crt_user     VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_merge_fs_crt_user DEFAULT(''),
    fd_crt_date     DATETIME NOT NULL CONSTRAINT DF_ta_trs_merge_fd_crt_date DEFAULT('3000-01-01'),
    fs_upd_user     VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_merge_fs_upd_user DEFAULT(''),
    fd_upd_date     DATETIME NOT NULL CONSTRAINT DF_ta_trs_merge_fd_upd_date DEFAULT('3000-01-01'),

    CONSTRAINT PK_ta_trs_merge PRIMARY KEY CLUSTERED (fs_kd_reg_merge, fs_kd_reg_asal)
)
GO

CREATE NONCLUSTERED INDEX IX_ta_trs_merge_fs_kd_reg_asal
    ON ta_trs_merge (fs_kd_reg_asal)
GO
