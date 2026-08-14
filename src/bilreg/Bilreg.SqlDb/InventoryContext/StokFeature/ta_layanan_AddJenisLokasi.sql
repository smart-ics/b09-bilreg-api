-- Extends Bilreg Admisi ta_layanan with pharmacy stock location type.
-- Keep Bilreg.SqlDb/AdmisiContext/LayananFeature/ta_layanan.sql as the CREATE script.
IF COL_LENGTH('dbo.ta_layanan', 'fs_kd_jenis_lokasi') IS NULL
BEGIN
    ALTER TABLE dbo.ta_layanan
        ADD fs_kd_jenis_lokasi VARCHAR(3) NOT NULL
            CONSTRAINT DF_ta_layanan_fs_kd_jenis_lokasi DEFAULT('')
END
GO