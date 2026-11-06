----------------------------------------------------------------------------------------------------------------------------------
-- Migration Script: BILRG_TrsBilling_M3_TraseabilityIndex_Alter.sql
-- Phase: P3-S05 - Enriched Billing Item DTO & Net Financial Position
-- Description: Menambah indeks terintegrasi untuk query tracing DTO dan
--              kompatibilitas baca dua (dual-read) registrasi merge.
-- Catatan: Script ini ADDITIF dan IDEMPOTEN (hanya dieksekusi jika kolom belum ada).
--          Jangan dieksekusi langsung terhadap database live tanpa review.
----------------------------------------------------------------------------------------------------------------------------------

-- Index untuk performa query join ke ta_trs_merge (pencarian asc registrasi)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ta_trs_merge]') AND name = N'IX_ta_trs_merge_source_target')
CREATE NONCLUSTERED INDEX IX_ta_trs_merge_source_target
ON ta_trs_merge (fs_kd_reg_merge, fs_kd_reg_asal);
GO

-- Index untuk performa query pencarian billing2 berdasarkan registrasi sumber
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[ta_trs_billing2]') AND name = N'IX_ta_trs_billing2_reg_asal')
CREATE NONCLUSTERED INDEX IX_ta_trs_billing2_reg_asal
ON ta_trs_billing2 (fs_kd_reg);
GO

-- Komentar: Query TrBListBillingQuery sudah didesain untuk dual-read compatibility:
--   WHERE aa.fs_kd_reg = @RegId 
--      OR aa.fs_kd_reg IN (SELECT fs_kd_reg_asal FROM ta_trs_merge WHERE fs_kd_reg_merge = @RegId)
-- Indeks di atas akan mempercepat kedua pola query di atas.
----------------------------------------------------------------------------------------------------------------------------------