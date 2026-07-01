IF COL_LENGTH('ta_no_antrian_map_hdr', 'fs_kd_jadwal_harian') IS NULL
BEGIN
    ALTER TABLE ta_no_antrian_map_hdr
        ADD fs_kd_jadwal_harian VARCHAR(12) NULL;
END
GO
