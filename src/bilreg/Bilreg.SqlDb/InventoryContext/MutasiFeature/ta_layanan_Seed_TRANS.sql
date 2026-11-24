-- Master Data Seeding: In-Transit Location TRANS for ta_layanan
-- Bounded Context: Inventory / MutasiFeature
-- Idempotent script

IF NOT EXISTS (SELECT 1 FROM ta_layanan WHERE fs_kd_layanan = 'TRANS')
BEGIN
    INSERT INTO ta_layanan (fs_kd_layanan, fs_nm_layanan, fb_aktif)
    VALUES ('TRANS', 'IN TRANSIT MUTASI', 1);
END
