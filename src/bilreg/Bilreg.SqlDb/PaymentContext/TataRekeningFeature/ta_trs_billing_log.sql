--  Audit pembatalan finalisasi Tata Rekening.
--  Nama tabel dan kolom mengikuti skema legacy (ta_trs_*) agar konsisten dengan
--  ta_trs_billing / ta_trs_billing2 pada context PaymentContext.

IF OBJECT_ID('ta_trs_billing_log', 'U') IS NULL
BEGIN
    CREATE TABLE ta_trs_billing_log
    (
        fs_kd_log          VARCHAR(12) NOT NULL CONSTRAINT DF_ta_trs_billing_log_fs_kd_log DEFAULT(''),
        fs_kd_reg          VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_billing_log_fs_kd_reg DEFAULT(''),
        fs_user_spv        VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_billing_log_fs_user_spv DEFAULT(''),
        fs_user_aksi       VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_billing_log_fs_user_aksi DEFAULT(''),
        fd_tgl_batal       DATETIME NOT NULL CONSTRAINT DF_ta_trs_billing_log_fd_tgl_batal DEFAULT('3000-01-01'),
        fs_alasan_batal    NVARCHAR(500) NOT NULL CONSTRAINT DF_ta_trs_billing_log_fs_alasan_batal DEFAULT(''),
        fs_status_sebelum  VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing_log_fs_status_sebelum DEFAULT(''),
        fs_status_setelah  VARCHAR(20) NOT NULL CONSTRAINT DF_ta_trs_billing_log_fs_status_setelah DEFAULT(''),
        fs_ip_address      VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_billing_log_fs_ip_address DEFAULT(''),

        CONSTRAINT PK_ta_trs_billing_log PRIMARY KEY CLUSTERED (fs_kd_log)
    )
END
GO

IF COL_LENGTH('ta_trs_billing_log', 'fs_user_spv') IS NULL
BEGIN
    ALTER TABLE ta_trs_billing_log
        ADD fs_user_spv VARCHAR(50) NOT NULL
            CONSTRAINT DF_ta_trs_billing_log_fs_user_spv DEFAULT('')
END
GO

IF COL_LENGTH('ta_trs_billing_log', 'fs_user_aksi') IS NULL
BEGIN
    ALTER TABLE ta_trs_billing_log
        ADD fs_user_aksi VARCHAR(50) NOT NULL
            CONSTRAINT DF_ta_trs_billing_log_fs_user_aksi DEFAULT('')
END
GO

IF COL_LENGTH('ta_trs_billing_log', 'fd_tgl_batal') IS NULL
BEGIN
    ALTER TABLE ta_trs_billing_log
        ADD fd_tgl_batal DATETIME NOT NULL
            CONSTRAINT DF_ta_trs_billing_log_fd_tgl_batal DEFAULT('3000-01-01')
END
GO

IF COL_LENGTH('ta_trs_billing_log', 'fs_alasan_batal') IS NULL
BEGIN
    ALTER TABLE ta_trs_billing_log
        ADD fs_alasan_batal NVARCHAR(500) NOT NULL
            CONSTRAINT DF_ta_trs_billing_log_fs_alasan_batal DEFAULT('')
END
GO

IF COL_LENGTH('ta_trs_billing_log', 'fs_status_sebelum') IS NULL
BEGIN
    ALTER TABLE ta_trs_billing_log
        ADD fs_status_sebelum VARCHAR(20) NOT NULL
            CONSTRAINT DF_ta_trs_billing_log_fs_status_sebelum DEFAULT('')
END
GO

IF COL_LENGTH('ta_trs_billing_log', 'fs_status_setelah') IS NULL
BEGIN
    ALTER TABLE ta_trs_billing_log
        ADD fs_status_setelah VARCHAR(20) NOT NULL
            CONSTRAINT DF_ta_trs_billing_log_fs_status_setelah DEFAULT('')
END
GO

IF COL_LENGTH('ta_trs_billing_log', 'fs_ip_address') IS NULL
BEGIN
    ALTER TABLE ta_trs_billing_log
        ADD fs_ip_address VARCHAR(50) NOT NULL
            CONSTRAINT DF_ta_trs_billing_log_fs_ip_address DEFAULT('')
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ta_trs_billing_log_fs_kd_reg' AND object_id = OBJECT_ID('ta_trs_billing_log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ta_trs_billing_log_fs_kd_reg
        ON ta_trs_billing_log (fs_kd_reg, fd_tgl_batal)
END
GO
