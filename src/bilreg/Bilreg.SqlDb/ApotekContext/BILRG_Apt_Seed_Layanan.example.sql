-- EXAMPLE ONLY — replace LayananId / names with Ops-approved values.
-- Pharmacy Unit and Dispensing Temporary Unit are Stock Ledger locations (LayananId).
-- Idempotent. Do not run unchanged against production.

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM ta_layanan WHERE fs_kd_layanan = 'LYAPT')
BEGIN
    INSERT INTO ta_layanan (
        fs_kd_layanan, fs_nm_layanan, fb_aktif,
        fs_kd_instalasi, fs_kd_layanan_dk, fs_kd_layanan_tipe_dk)
    VALUES ('LYAPT', 'Apotek Rawat Jalan', 1, '', '', '');
END;

IF COL_LENGTH('dbo.ta_layanan', 'fs_kd_jenis_lokasi') IS NOT NULL
BEGIN
    UPDATE ta_layanan
    SET fs_kd_jenis_lokasi = 'APT'
    WHERE fs_kd_layanan = 'LYAPT'
      AND (fs_kd_jenis_lokasi = '' OR fs_kd_jenis_lokasi IS NULL);
END;

IF NOT EXISTS (SELECT 1 FROM ta_layanan WHERE fs_kd_layanan = 'LYDTU')
BEGIN
    INSERT INTO ta_layanan (
        fs_kd_layanan, fs_nm_layanan, fb_aktif,
        fs_kd_instalasi, fs_kd_layanan_dk, fs_kd_layanan_tipe_dk)
    VALUES ('LYDTU', 'Dispensing Temp Unit', 1, '', '', '');
END;

IF COL_LENGTH('dbo.ta_layanan', 'fs_kd_jenis_lokasi') IS NOT NULL
BEGIN
    UPDATE ta_layanan
    SET fs_kd_jenis_lokasi = 'APT'
    WHERE fs_kd_layanan = 'LYDTU'
      AND (fs_kd_jenis_lokasi = '' OR fs_kd_jenis_lokasi IS NULL);
END;
