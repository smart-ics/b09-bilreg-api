-- Stock Ledger Phase 0 — Read-only profiling pack
-- Target: production snapshot (e.g. HOSPITAL_HPL)
-- Safety: SELECT / metadata only. Do NOT run DDL/DML against production or snapshots used as authority.
-- No credentials in this file.

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT '=== P0-01 Row counts ===';
SELECT 'tb_stok' AS table_name, COUNT_BIG(*) AS row_count FROM dbo.tb_stok WITH (NOLOCK)
UNION ALL
SELECT 'tb_buku', COUNT_BIG(*) FROM dbo.tb_buku WITH (NOLOCK);

PRINT '=== P0-02 Column metadata tb_buku / tb_stok ===';
SELECT c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH,
       c.IS_NULLABLE, c.COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME IN ('tb_stok', 'tb_buku')
ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;

PRINT '=== P0-03 Indexes ===';
SELECT t.name AS table_name, i.name AS index_name, i.type_desc, i.is_unique, i.is_primary_key,
       STUFF((
           SELECT ',' + c2.name
           FROM sys.index_columns ic2
           INNER JOIN sys.columns c2 ON c2.object_id = ic2.object_id AND c2.column_id = ic2.column_id
           WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.is_included_column = 0
           ORDER BY ic2.key_ordinal
           FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, '') AS key_columns
FROM sys.tables t
INNER JOIN sys.indexes i ON i.object_id = t.object_id
WHERE t.name IN ('tb_stok', 'tb_buku') AND i.type > 0
ORDER BY t.name, i.is_primary_key DESC, i.name;

PRINT '=== P0-04 Triggers ===';
SELECT t.name AS table_name, tr.name AS trigger_name, tr.is_disabled, tr.is_instead_of_trigger
FROM sys.tables t
INNER JOIN sys.triggers tr ON tr.parent_id = t.object_id
WHERE t.name IN ('tb_stok', 'tb_buku')
ORDER BY t.name, tr.name;

PRINT '=== P0-05 Foreign keys referencing or from stock tables ===';
SELECT fk.name AS fk_name, OBJECT_NAME(fk.parent_object_id) AS parent_table,
       OBJECT_NAME(fk.referenced_object_id) AS referenced_table
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) IN ('tb_stok', 'tb_buku')
   OR OBJECT_NAME(fk.referenced_object_id) IN ('tb_stok', 'tb_buku');

PRINT '=== P0-06 Change Tracking / CDC status ===';
SELECT DB_NAME() AS db_name,
       DATABASEPROPERTYEX(DB_NAME(), 'IsChangeTrackingOn') AS is_change_tracking_on;
SELECT s.name AS schema_name, t.name AS table_name, ct.is_track_columns_updated_on
FROM sys.change_tracking_tables ct
INNER JOIN sys.tables t ON t.object_id = ct.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.name IN ('tb_stok', 'tb_buku');
SELECT s.name AS schema_name, t.name AS table_name, ct.capture_instance, ct.is_tracked_by_cdc
FROM cdc.change_tables ct
RIGHT JOIN sys.tables t ON t.object_id = ct.source_object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.name IN ('tb_stok', 'tb_buku');

PRINT '=== P0-07 fd_tgl_jam_mutasi population (FQ-01) ===';
SELECT
    COUNT_BIG(*) AS total_rows,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(fd_tgl_jam_mutasi)), '') IS NULL THEN 1 ELSE 0 END) AS null_or_blank,
    SUM(CASE WHEN LTRIM(RTRIM(fd_tgl_jam_mutasi)) = '3000-01-01 00:00:00' THEN 1 ELSE 0 END) AS default_sentinel,
    SUM(CASE WHEN TRY_CONVERT(datetime, fd_tgl_jam_mutasi, 120) IS NOT NULL
              AND LTRIM(RTRIM(fd_tgl_jam_mutasi)) <> '3000-01-01 00:00:00' THEN 1 ELSE 0 END) AS parseable_non_default,
    SUM(CASE WHEN TRY_CONVERT(datetime, fd_tgl_jam_mutasi, 120) IS NULL
              AND NULLIF(LTRIM(RTRIM(fd_tgl_jam_mutasi)), '') IS NOT NULL
              AND LTRIM(RTRIM(fd_tgl_jam_mutasi)) <> '3000-01-01 00:00:00' THEN 1 ELSE 0 END) AS unparseable_non_default,
    MIN(CASE WHEN TRY_CONVERT(datetime, fd_tgl_jam_mutasi, 120) IS NOT NULL
              AND YEAR(TRY_CONVERT(datetime, fd_tgl_jam_mutasi, 120)) < 2999
             THEN fd_tgl_jam_mutasi END) AS min_mutasi,
    MAX(CASE WHEN TRY_CONVERT(datetime, fd_tgl_jam_mutasi, 120) IS NOT NULL
              AND YEAR(TRY_CONVERT(datetime, fd_tgl_jam_mutasi, 120)) < 2999
             THEN fd_tgl_jam_mutasi END) AS max_mutasi
FROM dbo.tb_buku WITH (NOLOCK);

PRINT '=== P0-08 Separate date/time fields vs combined ===';
SELECT
    COUNT_BIG(*) AS total_rows,
    SUM(CASE WHEN LTRIM(RTRIM(fd_tgl_mutasi)) = '3000-01-01' OR NULLIF(LTRIM(RTRIM(fd_tgl_mutasi)),'') IS NULL THEN 1 ELSE 0 END) AS tgl_default_or_blank,
    SUM(CASE WHEN LTRIM(RTRIM(fs_jam_mutasi)) IN ('00:00:00','') OR NULLIF(LTRIM(RTRIM(fs_jam_mutasi)),'') IS NULL THEN 1 ELSE 0 END) AS jam_default_or_blank,
    SUM(CASE WHEN LTRIM(RTRIM(fd_tgl_mutasi)) + ' ' + LTRIM(RTRIM(fs_jam_mutasi)) = LTRIM(RTRIM(fd_tgl_jam_mutasi)) THEN 1 ELSE 0 END) AS combined_matches_parts,
    SUM(CASE WHEN LTRIM(RTRIM(fd_tgl_mutasi)) + ' ' + LTRIM(RTRIM(fs_jam_mutasi)) <> LTRIM(RTRIM(fd_tgl_jam_mutasi))
              AND LTRIM(RTRIM(fd_tgl_jam_mutasi)) <> '3000-01-01 00:00:00'
              AND LTRIM(RTRIM(fd_tgl_mutasi)) <> '3000-01-01' THEN 1 ELSE 0 END) AS combined_mismatch_nonzero
FROM dbo.tb_buku WITH (NOLOCK);

PRINT '=== P0-09 Timestamp ties (same fd_tgl_jam_mutasi, multiple fs_kd_trs) ===';
SELECT TOP 20
    fd_tgl_jam_mutasi,
    COUNT(*) AS row_count,
    COUNT(DISTINCT fs_kd_trs) AS distinct_trs
FROM dbo.tb_buku WITH (NOLOCK)
WHERE LTRIM(RTRIM(fd_tgl_jam_mutasi)) <> '3000-01-01 00:00:00'
  AND NULLIF(LTRIM(RTRIM(fd_tgl_jam_mutasi)), '') IS NOT NULL
GROUP BY fd_tgl_jam_mutasi
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

PRINT '=== P0-10 Backdating / ID-vs-time inversions (bounded hot scopes) ===';
-- Limit to hottest Item+DO scopes to avoid full-table self-join cost on multi-million histories.
;WITH hot AS (
    SELECT TOP 50 fs_kd_barang, fs_kd_do
    FROM dbo.tb_buku WITH (NOLOCK)
    WHERE NULLIF(LTRIM(RTRIM(fs_kd_do)), '') IS NOT NULL
    GROUP BY fs_kd_barang, fs_kd_do
    ORDER BY COUNT_BIG(*) DESC
),
scoped AS (
    SELECT b.fs_kd_barang, b.fs_kd_do, b.fs_kd_trs, b.fd_tgl_jam_mutasi,
           TRY_CONVERT(datetime, b.fd_tgl_jam_mutasi, 120) AS mutasi_dt,
           ROW_NUMBER() OVER (PARTITION BY b.fs_kd_barang, b.fs_kd_do ORDER BY b.fs_kd_trs) AS rn_trs,
           ROW_NUMBER() OVER (PARTITION BY b.fs_kd_barang, b.fs_kd_do
                              ORDER BY TRY_CONVERT(datetime, b.fd_tgl_jam_mutasi, 120), b.fs_kd_trs) AS rn_time
    FROM dbo.tb_buku b WITH (NOLOCK)
    INNER JOIN hot h ON h.fs_kd_barang = b.fs_kd_barang AND h.fs_kd_do = b.fs_kd_do
    WHERE LTRIM(RTRIM(b.fd_tgl_jam_mutasi)) <> '3000-01-01 00:00:00'
)
SELECT
    COUNT(*) AS rows_in_hot_scopes,
    SUM(CASE WHEN rn_trs <> rn_time THEN 1 ELSE 0 END) AS rows_where_trs_order_differs_from_time_order
FROM scoped;

PRINT '=== P0-11 fs_kd_trs length / prefix patterns (FQ-02) ===';
SELECT LEN(LTRIM(RTRIM(fs_kd_trs))) AS trs_len, COUNT_BIG(*) AS cnt
FROM dbo.tb_buku WITH (NOLOCK)
GROUP BY LEN(LTRIM(RTRIM(fs_kd_trs)))
ORDER BY cnt DESC;

SELECT LEFT(LTRIM(RTRIM(fs_kd_trs)), 2) AS prefix2, COUNT_BIG(*) AS cnt
FROM dbo.tb_buku WITH (NOLOCK)
GROUP BY LEFT(LTRIM(RTRIM(fs_kd_trs)), 2)
ORDER BY cnt DESC;

PRINT '=== P0-12 Hottest Item + Receipt Source histories (FQ-05) ===';
SELECT TOP 20
    fs_kd_barang, fs_kd_do,
    COUNT_BIG(*) AS buku_rows,
    COUNT(DISTINCT fs_kd_layanan) AS location_count,
    MIN(fd_tgl_jam_mutasi) AS min_mutasi,
    MAX(fd_tgl_jam_mutasi) AS max_mutasi
FROM dbo.tb_buku WITH (NOLOCK)
WHERE NULLIF(LTRIM(RTRIM(fs_kd_do)), '') IS NOT NULL
GROUP BY fs_kd_barang, fs_kd_do
ORDER BY COUNT_BIG(*) DESC;

PRINT '=== P0-13 Mutation type distribution ===';
SELECT LTRIM(RTRIM(fs_kd_jenis_mutasi)) AS jenis_mutasi, COUNT_BIG(*) AS cnt
FROM dbo.tb_buku WITH (NOLOCK)
GROUP BY LTRIM(RTRIM(fs_kd_jenis_mutasi))
ORDER BY cnt DESC;

PRINT '=== P0-14 tb_stok vs tb_buku DO presence ===';
SELECT
    (SELECT COUNT_BIG(*) FROM dbo.tb_stok WITH (NOLOCK)) AS stok_rows,
    (SELECT COUNT_BIG(*) FROM dbo.tb_stok WITH (NOLOCK) WHERE fn_qty = 0) AS stok_zero_qty,
    (SELECT COUNT_BIG(*) FROM dbo.tb_stok WITH (NOLOCK) WHERE NULLIF(LTRIM(RTRIM(fs_kd_do)),'') IS NOT NULL) AS stok_with_do,
    (SELECT COUNT(DISTINCT fs_kd_barang + '|' + fs_kd_do) FROM dbo.tb_buku WITH (NOLOCK) WHERE NULLIF(LTRIM(RTRIM(fs_kd_do)),'') IS NOT NULL) AS distinct_buku_item_do,
    (SELECT COUNT(DISTINCT fs_kd_barang + '|' + fs_kd_do) FROM dbo.tb_stok WITH (NOLOCK) WHERE NULLIF(LTRIM(RTRIM(fs_kd_do)),'') IS NOT NULL) AS distinct_stok_item_do;

PRINT '=== P0-15 Sample: buku rows without matching current stok for same barang+layanan+do (depletion signal) ===';
SELECT TOP 10
    b.fs_kd_barang, b.fs_kd_do, b.fs_kd_layanan,
    COUNT(*) AS buku_rows,
    SUM(b.fn_stok_in) AS sum_in,
    SUM(b.fn_stok_out) AS sum_out,
    MAX(CASE WHEN s.fs_kd_trs IS NULL THEN 0 ELSE 1 END) AS has_any_stok_row
FROM dbo.tb_buku b WITH (NOLOCK)
LEFT JOIN dbo.tb_stok s WITH (NOLOCK)
    ON s.fs_kd_barang = b.fs_kd_barang
   AND s.fs_kd_layanan = b.fs_kd_layanan
   AND s.fs_kd_do = b.fs_kd_do
WHERE NULLIF(LTRIM(RTRIM(b.fs_kd_do)), '') IS NOT NULL
GROUP BY b.fs_kd_barang, b.fs_kd_do, b.fs_kd_layanan
HAVING MAX(CASE WHEN s.fs_kd_trs IS NULL THEN 0 ELSE 1 END) = 0
   AND SUM(b.fn_stok_in) > 0
ORDER BY SUM(b.fn_stok_out) DESC;

PRINT '=== P0-16 Related stock-ish tables present ===';
SELECT name
FROM sys.tables
WHERE name LIKE '%stok%' OR name LIKE '%buku%' OR name LIKE 'FARIN_Stok%'
ORDER BY name;

PRINT '=== Done ===';
