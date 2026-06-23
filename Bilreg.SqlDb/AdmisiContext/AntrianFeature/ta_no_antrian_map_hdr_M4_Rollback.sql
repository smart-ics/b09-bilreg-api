IF COL_LENGTH('ta_no_antrian_map_hdr', 'fs_kd_jadwal_harian') IS NOT NULL
BEGIN
    ALTER TABLE ta_no_antrian_map_hdr DROP COLUMN fs_kd_jadwal_harian;
END
GO
