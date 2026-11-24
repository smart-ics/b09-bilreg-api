/*
====================================================================================================
Migration: ta_no_antrian_map_M1_UniqueTrsJadwal_Alter.sql
Feature  : AdmisiContext / AntrianFeature
Issue Ref: ISS-ADM-REG-017 / ADM-REG-ANTRIAN-MAP-DUPLICATE
Purpose  : Menambahkan Filtered Unique Index pada tabel ta_no_antrian_map untuk mencegah duplikasi
           alokasi slot nomor antrian aktif (fs_kd_trs_gen / RG...) pada satu sesi jadwal praktek dokter.
Sintaks  : SQL Server 2016+
====================================================================================================

----------------------------------------------------------------------------------------------------
PANDUAN PEMBERSIHAN DATA DUPLIKAT HISTORIS BAGI TIM EDP / IT (PRE-MIGRATION DATA CLEANSING)
----------------------------------------------------------------------------------------------------
Sebelum index unik di bawah diterapkan, tabel dbo.ta_no_antrian_map harus bebas dari data duplikat aktif.
Gunakan langkah-langkah query berikut jika migrasi gagal akibat pelanggaran constraint unik:

1. Deteksi transaksi pendaftaran yang menempati lebih dari 1 slot aktif pada sesi jadwal yang sama:
   SELECT 
       fs_kd_dokter, 
       fs_kd_layanan, 
       fd_tgl_jadwal, 
       fs_jam_jadwal, 
       fs_kd_trs_gen, 
       COUNT(*) AS total_slot_aktif
   FROM dbo.ta_no_antrian_map
   WHERE fs_kd_trs_gen <> '' AND fb_terpakai = 1
   GROUP BY fs_kd_dokter, fs_kd_layanan, fd_tgl_jadwal, fs_jam_jadwal, fs_kd_trs_gen
   HAVING COUNT(*) > 1;

2. Tinjau rincian baris dan nomor urut antrian (fn_no_antrian) yang terduplikasi:
   WITH DuplicateSlotsCTE AS (
       SELECT 
           fs_kd_antrian_map,
           fn_no_antrian,
           fs_kd_dokter,
           fs_kd_layanan,
           fd_tgl_jadwal,
           fs_jam_jadwal,
           fs_kd_trs_gen,
           fs_mr,
           FS_NM_PASIEN,
           fb_terpakai,
           ROW_NUMBER() OVER (
               PARTITION BY fs_kd_dokter, fs_kd_layanan, fd_tgl_jadwal, fs_jam_jadwal, fs_kd_trs_gen
               ORDER BY fn_no_antrian ASC -- nomor antrian pertama diasumsikan nomor awal
           ) AS SlotRank
       FROM dbo.ta_no_antrian_map
       WHERE fs_kd_trs_gen <> '' AND fb_terpakai = 1
   )
   SELECT * 
   FROM DuplicateSlotsCTE 
   WHERE SlotRank > 1;

3. Rekonsiliasi / Pembersihan: Kosongkan slot duplikat berlebih (SlotRank > 1):
   -- CATATAN: Pastikan mencocokkan nomor antrian yang sah di BILRG_Antrian sebelum mengeksekusi UPDATE.
   WITH DuplicateSlotsCTE AS (
       SELECT 
           fs_kd_antrian_map,
           fn_no_antrian,
           fs_kd_dokter,
           fs_kd_layanan,
           fd_tgl_jadwal,
           fs_jam_jadwal,
           fs_kd_trs_gen,
           fs_mr,
           FS_NM_PASIEN,
           fb_terpakai,
           ROW_NUMBER() OVER (
               PARTITION BY fs_kd_dokter, fs_kd_layanan, fd_tgl_jadwal, fs_jam_jadwal, fs_kd_trs_gen
               ORDER BY fn_no_antrian ASC
           ) AS SlotRank
       FROM dbo.ta_no_antrian_map
       WHERE fs_kd_trs_gen <> '' AND fb_terpakai = 1
   )
   UPDATE DuplicateSlotsCTE
   SET fb_terpakai    = 0,
       fs_kd_trs_gen  = '',
       fs_ket_trs_gen = '',
       fs_mr          = '',
       FS_NM_PASIEN   = '',
       UPDTGL         = CONVERT(VARCHAR(10), GETDATE(), 120),
       UPDJAM         = CONVERT(VARCHAR(8), GETDATE(), 108),
       UPDUSR         = 'EDP_CLEAN'
   WHERE SlotRank > 1;
----------------------------------------------------------------------------------------------------
*/

-- Penerapan Filtered Unique Index dengan proteksi IF NOT EXISTS (Idempotent)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_ta_no_antrian_map_unique_trs_jadwal' 
      AND object_id = OBJECT_ID('dbo.ta_no_antrian_map')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ta_no_antrian_map_unique_trs_jadwal
    ON dbo.ta_no_antrian_map (
        fs_kd_dokter,
        fs_kd_layanan,
        fd_tgl_jadwal,
        fs_jam_jadwal,
        fs_kd_trs_gen
    )
    WHERE fs_kd_trs_gen <> '' AND fb_terpakai = 1;
END
GO
