/* ==============================================================================================
   SCRIPT: BILRG_LabTestDefinition_AuditMasterTarif.sql
   CONTEXT: LabContext / LabTestDefinitionFeature
   TARGET REPOSITORY: b09-bilreg-api
   TARGET DATABASE: HOSPITAL_PKL / BILREG_DEV

   PURPOSE:
   Mengaudit kelengkapan data master BILRG_LabTestDefinition dan BILRG_LabTestComponent di database
   Bilreg terhadap seluruh tarif aktif yang digunakan pada formulir pemesanan EMR (TA_TARIF / TRK-TARIF).
   
   Tujuan utama audit:
   1. Mencegah timbulnya exception runtime pada handler LabOrderCreateFromEmrHandler:
      - LAB_TEST_DEFINITION_NOT_FOUND (TarifId tidak ditemukan pada BILRG_LabTestDefinition)
      - LAB_TEST_DEFINITION_INACTIVE  (BILRG_LabTestDefinition ada tetapi IsActive = 0)
      - LAB_TEST_DEFINITION_EMPTY_COMPONENTS (IsActive = 1 tetapi tidak memiliki komponen)
      - LAB_COMPONENT_NOT_FOUND / LAB_COMPONENT_INACTIVE (Komponen mengacu ke MLC yang tidak valid/inaktif)
      - LAB_DUPLICATE_SEQUENCE / LAB_DUPLICATE_COMPONENT (Integritas urutan / keunikan komponen)
   2. Memberikan visualisasi kesiapan panel uji rutin laboratorium (CBC, Kimia, Urinalisis, Serologi, dll.)
   3. Memberikan rekomendasi remediasi dan template SQL untuk perbaikan tarif unmapped / inaktif.

   EXECUTION:
   Dapat dijalankan kapan saja secara read-only (aman, tanpa modifikasi data).
============================================================================================== */

SET NOCOUNT ON;

-- ==============================================================================================
-- 0. FILTER PARAMETERS
-- ==============================================================================================
DECLARE @TarifPrefixFilter VARCHAR(20) = 'LAB%'; -- Filter kode tarif lab (default 'LAB%')
DECLARE @OnlyDefects       BIT         = 0;      -- 1 = Hanya tampilkan tarif yang bermasalah / belum siap
                                                 -- 0 = Tampilkan seluruh tarif lab dan status lengkap

-- ==============================================================================================
-- 1. IDENTIFY LAB TARIFS & RESOLUTION BASELINE
-- ==============================================================================================
IF OBJECT_ID('tempdb..#LabTarifCatalog') IS NOT NULL DROP TABLE #LabTarifCatalog;
CREATE TABLE #LabTarifCatalog (
    TarifId          VARCHAR(12)  NOT NULL PRIMARY KEY,
    TarifName        VARCHAR(100) NOT NULL,
    HasLegacyConfig  BIT          NOT NULL DEFAULT(0),
    LegacyItemCount  INT          NOT NULL DEFAULT(0)
);

-- Sumber 1: Master Tarif Aktif (TA_TARIF) dengan prefix tarif laboratorium
INSERT INTO #LabTarifCatalog (TarifId, TarifName, HasLegacyConfig, LegacyItemCount)
SELECT 
    RTRIM(LTRIM(t.fs_kd_tarif)) AS TarifId,
    RTRIM(LTRIM(t.fs_nm_tarif)) AS TarifName,
    CASE WHEN COUNT(t6.fs_kd_jenis_pemeriksaan) > 0 THEN 1 ELSE 0 END AS HasLegacyConfig,
    COUNT(t6.fs_kd_jenis_pemeriksaan) AS LegacyItemCount
FROM ta_tarif t
LEFT JOIN TA_TARIF6 t6 ON t.fs_kd_tarif = t6.fs_kd_tarif
WHERE t.fs_kd_tarif LIKE @TarifPrefixFilter
   OR t.fs_kd_tarif IN (SELECT TarifId FROM BILRG_LabTestDefinition)
GROUP BY t.fs_kd_tarif, t.fs_nm_tarif;

-- Jika tabel ta_tarif kosong (misal environment isolated/unit test database), 
-- gunakan tarif yang tercatat di BILRG_LabTestDefinition sebagai katalog evaluasi.
IF NOT EXISTS (SELECT 1 FROM #LabTarifCatalog)
BEGIN
    INSERT INTO #LabTarifCatalog (TarifId, TarifName, HasLegacyConfig, LegacyItemCount)
    SELECT 
        ltd.TarifId,
        ltd.TarifName,
        1 AS HasLegacyConfig,
        (SELECT COUNT(*) FROM BILRG_LabTestComponent WHERE TestDefinitionId = ltd.TestDefinitionId) AS LegacyItemCount
    FROM BILRG_LabTestDefinition ltd;
END

-- ==============================================================================================
-- 2. EVALUATE DEFINITION & COMPONENT INTEGRITY
-- ==============================================================================================
IF OBJECT_ID('tempdb..#DefinitionEvaluation') IS NOT NULL DROP TABLE #DefinitionEvaluation;
CREATE TABLE #DefinitionEvaluation (
    TarifId                    VARCHAR(12)  NOT NULL,
    TarifName                  VARCHAR(100) NOT NULL,
    TestDefinitionId           VARCHAR(7)   NULL,
    LabTestCode                VARCHAR(20)  NULL,
    LabTestName                VARCHAR(100) NULL,
    SpecimenType               VARCHAR(30)  NULL,
    VacutainerType             INT          NULL,
    VacutainerName             VARCHAR(30)  NULL,
    DefinitionIsActive         BIT          NULL,
    TotalComponents            INT          NOT NULL DEFAULT(0),
    ActiveMasterComponents     INT          NOT NULL DEFAULT(0),
    InactiveMasterComponents   INT          NOT NULL DEFAULT(0),
    MissingMasterComponents    INT          NOT NULL DEFAULT(0),
    DuplicateSequenceCount     INT          NOT NULL DEFAULT(0),
    DuplicateComponentCount    INT          NOT NULL DEFAULT(0),
    OverallStatus              VARCHAR(25)  NOT NULL,
    PotentialRuntimeError      VARCHAR(50)  NOT NULL,
    RemediationAction          VARCHAR(250) NOT NULL
);

INSERT INTO #DefinitionEvaluation
SELECT 
    c.TarifId,
    c.TarifName,
    ltd.TestDefinitionId,
    ltd.LabTestCode,
    ltd.LabTestName,
    ltd.SpecimenType,
    ltd.VacutainerType,
    CASE ltd.VacutainerType
        WHEN 0 THEN '0: None / Pot'
        WHEN 1 THEN '1: EDTA (Ungu)'
        WHEN 2 THEN '2: Serum (Kuning/Merah)'
        WHEN 3 THEN '3: Citrate (Biru)'
        WHEN 4 THEN '4: Heparin (Hijau)'
        ELSE ISNULL(CONVERT(VARCHAR(10), ltd.VacutainerType), 'N/A')
    END AS VacutainerName,
    ltd.IsActive AS DefinitionIsActive,
    ISNULL(comp_stats.TotalComponents, 0) AS TotalComponents,
    ISNULL(comp_stats.ActiveMasterComponents, 0) AS ActiveMasterComponents,
    ISNULL(comp_stats.InactiveMasterComponents, 0) AS InactiveMasterComponents,
    ISNULL(comp_stats.MissingMasterComponents, 0) AS MissingMasterComponents,
    ISNULL(comp_stats.DuplicateSequenceCount, 0) AS DuplicateSequenceCount,
    ISNULL(comp_stats.DuplicateComponentCount, 0) AS DuplicateComponentCount,
    
    -- Status Penilaian
    CASE 
        WHEN ltd.TestDefinitionId IS NULL THEN 'UNMAPPED'
        WHEN ltd.IsActive = 0 THEN 'INACTIVE_DRAFT'
        WHEN ISNULL(comp_stats.TotalComponents, 0) = 0 THEN 'EMPTY_COMPONENTS'
        WHEN ISNULL(comp_stats.MissingMasterComponents, 0) > 0 THEN 'INVALID_MLC_REFERENCE'
        WHEN ISNULL(comp_stats.InactiveMasterComponents, 0) > 0 THEN 'INACTIVE_MLC_REFERENCE'
        WHEN ISNULL(comp_stats.DuplicateSequenceCount, 0) > 0 OR ISNULL(comp_stats.DuplicateComponentCount, 0) > 0 THEN 'STRUCTURAL_DEFECT'
        ELSE 'ALIGNED_READY'
    END AS OverallStatus,

    -- Potensi Runtime Exception pada use case LabOrderCreateFromEmrCmd
    CASE 
        WHEN ltd.TestDefinitionId IS NULL THEN 'LAB_TEST_DEFINITION_NOT_FOUND'
        WHEN ltd.IsActive = 0 THEN 'LAB_TEST_DEFINITION_INACTIVE'
        WHEN ISNULL(comp_stats.TotalComponents, 0) = 0 THEN 'LAB_TEST_DEFINITION_EMPTY_COMPONENTS'
        WHEN ISNULL(comp_stats.MissingMasterComponents, 0) > 0 THEN 'LAB_COMPONENT_NOT_FOUND'
        WHEN ISNULL(comp_stats.InactiveMasterComponents, 0) > 0 THEN 'LAB_COMPONENT_INACTIVE'
        WHEN ISNULL(comp_stats.DuplicateSequenceCount, 0) > 0 THEN 'LAB_DUPLICATE_SEQUENCE'
        WHEN ISNULL(comp_stats.DuplicateComponentCount, 0) > 0 THEN 'LAB_DUPLICATE_COMPONENT'
        ELSE 'NONE (SAFE)'
    END AS PotentialRuntimeError,

    -- Tindakan Remediasi
    CASE 
        WHEN ltd.TestDefinitionId IS NULL THEN 'Daftarkan entitas BILRG_LabTestDefinition dan komponen analis BILRG_LabTestComponent.'
        WHEN ltd.IsActive = 0 THEN 'Lengkapi seluruh pemetaan komponen ke BILRG_LabComponentMaster, lalu set IsActive = 1.'
        WHEN ISNULL(comp_stats.TotalComponents, 0) = 0 THEN 'Tambahkan minimal 1 baris komponen pemeriksaan pada BILRG_LabTestComponent.'
        WHEN ISNULL(comp_stats.MissingMasterComponents, 0) > 0 THEN 'Perbaiki foreign key ComponentId agar merujuk ke record BILRG_LabComponentMaster yang sah.'
        WHEN ISNULL(comp_stats.InactiveMasterComponents, 0) > 0 THEN 'Aktifkan master analit pada BILRG_LabComponentMaster (IsActive = 1).'
        WHEN ISNULL(comp_stats.DuplicateSequenceCount, 0) > 0 THEN 'Rapikan nomor urut SequenceNo agar berurutan 1..N tanpa duplikasi.'
        WHEN ISNULL(comp_stats.DuplicateComponentCount, 0) > 0 THEN 'Hapus duplikasi ComponentId dalam satu TestDefinitionId.'
        ELSE 'Siap digunakan untuk transaksi pemesanan EMR.'
    END AS RemediationAction

FROM #LabTarifCatalog c
LEFT JOIN BILRG_LabTestDefinition ltd ON c.TarifId = ltd.TarifId
OUTER APPLY (
    SELECT 
        COUNT(*) AS TotalComponents,
        SUM(CASE WHEN cm.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveMasterComponents,
        SUM(CASE WHEN cm.ComponentId IS NOT NULL AND cm.IsActive = 0 THEN 1 ELSE 0 END) AS InactiveMasterComponents,
        SUM(CASE WHEN cm.ComponentId IS NULL THEN 1 ELSE 0 END) AS MissingMasterComponents,
        COUNT(*) - COUNT(DISTINCT tc.SequenceNo) AS DuplicateSequenceCount,
        COUNT(*) - COUNT(DISTINCT tc.ComponentId) AS DuplicateComponentCount
    FROM BILRG_LabTestComponent tc
    LEFT JOIN BILRG_LabComponentMaster cm ON tc.ComponentId = cm.ComponentId
    WHERE tc.TestDefinitionId = ltd.TestDefinitionId
) comp_stats;

-- ==============================================================================================
-- 3. AUDIT REPORT SECTION 1: EXECUTIVE KPI & ALIGNMENT HEALTH SCORECARD
-- ==============================================================================================
SELECT 
    '=== LAB MASTER ALIGNMENT HEALTH SCORECARD ===' AS ReportSection,
    COUNT(*) AS TotalLabTarifsEvaluated,
    SUM(CASE WHEN OverallStatus = 'ALIGNED_READY' THEN 1 ELSE 0 END) AS ReadyActiveCount,
    SUM(CASE WHEN OverallStatus = 'INACTIVE_DRAFT' THEN 1 ELSE 0 END) AS InactiveDraftCount,
    SUM(CASE WHEN OverallStatus = 'UNMAPPED' THEN 1 ELSE 0 END) AS UnmappedCount,
    SUM(CASE WHEN OverallStatus IN ('EMPTY_COMPONENTS', 'INVALID_MLC_REFERENCE', 'INACTIVE_MLC_REFERENCE', 'STRUCTURAL_DEFECT') THEN 1 ELSE 0 END) AS DefectiveCount,
    CONVERT(DECIMAL(5, 2), 
        (SUM(CASE WHEN OverallStatus = 'ALIGNED_READY' THEN 1.0 ELSE 0.0 END) / NULLIF(COUNT(*), 0)) * 100
    ) AS ReadinessPercentage,
    CASE 
        WHEN SUM(CASE WHEN OverallStatus != 'ALIGNED_READY' THEN 1 ELSE 0 END) = 0 THEN 'EXCELLENT: 100% Siap Digunakan EMR'
        WHEN SUM(CASE WHEN OverallStatus = 'UNMAPPED' THEN 1 ELSE 0 END) > 0 THEN 'WARNING: Terdapat Tarif Unmapped Berisiko Error 400/500 di EMR'
        ELSE 'ATTENTION: Perlu Review Tarif Draft / Inactive'
    END AS AuditVerdict
FROM #DefinitionEvaluation;

-- ==============================================================================================
-- 4. AUDIT REPORT SECTION 2: HIGH-RISK AUDIT — UNMAPPED TARIFS (POTENTIAL NOT_FOUND)
-- ==============================================================================================
SELECT 
    '=== HIGH-RISK: UNMAPPED ACTIVE TARIFS (NO LabTestDefinition) ===' AS ReportSection,
    e.TarifId,
    e.TarifName,
    e.PotentialRuntimeError,
    e.RemediationAction
FROM #DefinitionEvaluation e
WHERE e.OverallStatus = 'UNMAPPED'
ORDER BY e.TarifId;

-- ==============================================================================================
-- 5. AUDIT REPORT SECTION 3: INACTIVE / DRAFT DEFINITIONS AUDIT (IsActive = 0)
-- ==============================================================================================
SELECT 
    '=== INACTIVE / DRAFT DEFINITIONS (IsActive = 0) ===' AS ReportSection,
    e.TarifId,
    e.TarifName,
    e.TestDefinitionId,
    e.TotalComponents,
    e.ActiveMasterComponents,
    e.MissingMasterComponents,
    e.PotentialRuntimeError,
    e.RemediationAction
FROM #DefinitionEvaluation e
WHERE e.OverallStatus = 'INACTIVE_DRAFT'
ORDER BY e.TarifId;

-- ==============================================================================================
-- 6. AUDIT REPORT SECTION 4: STRUCTURAL INVARIANT DEFECTS AUDIT
-- ==============================================================================================
SELECT 
    '=== STRUCTURAL INVARIANT DEFECTS (Active but Malformed) ===' AS ReportSection,
    e.TarifId,
    e.TarifName,
    e.TestDefinitionId,
    e.OverallStatus,
    e.TotalComponents,
    e.MissingMasterComponents,
    e.InactiveMasterComponents,
    e.DuplicateSequenceCount,
    e.DuplicateComponentCount,
    e.PotentialRuntimeError,
    e.RemediationAction
FROM #DefinitionEvaluation e
WHERE e.OverallStatus IN ('EMPTY_COMPONENTS', 'INVALID_MLC_REFERENCE', 'INACTIVE_MLC_REFERENCE', 'STRUCTURAL_DEFECT')
ORDER BY e.TarifId;

-- ==============================================================================================
-- 7. AUDIT REPORT SECTION 5: ROUTINE LAB CLINICAL PANEL COVERAGE CHECKLIST
-- ==============================================================================================
IF OBJECT_ID('tempdb..#RoutineClinicalPanels') IS NOT NULL DROP TABLE #RoutineClinicalPanels;
CREATE TABLE #RoutineClinicalPanels (
    PanelCode        VARCHAR(20)  NOT NULL PRIMARY KEY,
    PanelName        VARCHAR(60)  NOT NULL,
    SearchKeywords   VARCHAR(100) NOT NULL,
    ExpectedVacutainer INT        NOT NULL,
    ExpectedSpecimen   VARCHAR(20) NOT NULL
);

INSERT INTO #RoutineClinicalPanels (PanelCode, PanelName, SearchKeywords, ExpectedVacutainer, ExpectedSpecimen) VALUES
('DARAH_RUTIN',    'Hematologi / Darah Rutin (CBC)',       '%DARAH%RUTIN%|%DARAH%LENGKAP%|%HEMATOLOGI%', 1, 'Darah'),
('PROFIL_LIPID',   'Profil Lipid Lengkap',                 '%LIPID%|%KOLESTEROL%|%TRIGLISERID%',        2, 'Darah'),
('FUNGSI_HATI',    'Fungsi Hati (SGOT / SGPT / Bilirubin)', '%SGOT%|%SGPT%|%LIVER%|%HEPATIC%',            2, 'Darah'),
('FUNGSI_GINJAL',  'Fungsi Ginjal (Ureum / Kreatinin)',    '%UREUM%|%KREATININ%|%RENAL%',               2, 'Darah'),
('GLUKOSA',        'Glukosa Darah (Puasa / 2 Jam PP / GDS)', '%GLUKOSA%|%GDS%|%GDP%|%HBA1C%',            2, 'Darah'),
('URINALISIS',     'Urinalisis Lengkap',                   '%URIN%RUTIN%|%URINALISIS%',                 0, 'Urine'),
('ELEKTROLIT',     'Elektrolit Darah (Na, K, Cl)',         '%ELEKTROLIT%|%NATRIUM%|%KALIUM%',           2, 'Darah'),
('IMUNOSEROLOGI',  'Imunoserologi (HBsAg / HIV / Widal)',   '%HBSAG%|%HIV%|%WIDAL%|%DENGUE%',            2, 'Darah'),
('HEMOSTASIS',     'Hemostasis (Masa Pendarahan / PT / APTT)', '%MASA PENDARAHAN%|%PT%|%APTT%|%FIBRIN%', 3, 'Darah');

SELECT 
    '=== ROUTINE LAB CLINICAL PANEL COVERAGE CHECKLIST ===' AS ReportSection,
    p.PanelCode,
    p.PanelName,
    COUNT(e.TarifId) AS MatchedTarifsCount,
    SUM(CASE WHEN e.OverallStatus = 'ALIGNED_READY' THEN 1 ELSE 0 END) AS AlignedReadyCount,
    SUM(CASE WHEN e.OverallStatus != 'ALIGNED_READY' THEN 1 ELSE 0 END) AS UnalignedCount,
    CASE 
        WHEN SUM(CASE WHEN e.OverallStatus = 'ALIGNED_READY' THEN 1 ELSE 0 END) > 0 THEN 'COVERED (SIAP EMR)'
        WHEN COUNT(e.TarifId) > 0 THEN 'NEEDS_ALIGNMENT (Ada Tarif tapi Belum Siap)'
        ELSE 'NOT_CONFIGURED (Katalog Belum Ditemukan)'
    END AS PanelReadinessStatus
FROM #RoutineClinicalPanels p
LEFT JOIN #DefinitionEvaluation e 
    ON (e.TarifName LIKE '%' + REPLACE(p.SearchKeywords, '|', '% OR e.TarifName LIKE %') + '%')
    OR (p.PanelCode = 'DARAH_RUTIN'   AND (e.TarifName LIKE '%DARAH RUTIN%' OR e.TarifName LIKE '%DARAH LENGKAP%' OR e.TarifName LIKE '%HEMATO%'))
    OR (p.PanelCode = 'PROFIL_LIPID'  AND (e.TarifName LIKE '%LIPID%' OR e.TarifName LIKE '%KOLESTEROL%' OR e.TarifName LIKE '%TRIGLI%'))
    OR (p.PanelCode = 'FUNGSI_HATI'   AND (e.TarifName LIKE '%SGOT%' OR e.TarifName LIKE '%SGPT%' OR e.TarifName LIKE '%BILIRUBIN%'))
    OR (p.PanelCode = 'FUNGSI_GINJAL' AND (e.TarifName LIKE '%UREUM%' OR e.TarifName LIKE '%KREATININ%'))
    OR (p.PanelCode = 'GLUKOSA'       AND (e.TarifName LIKE '%GLUKOSA%' OR e.TarifName LIKE '%GDS%' OR e.TarifName LIKE '%HBA1C%'))
    OR (p.PanelCode = 'URINALISIS'    AND (e.TarifName LIKE '%URIN%'))
    OR (p.PanelCode = 'ELEKTROLIT'    AND (e.TarifName LIKE '%ELEKTROLIT%' OR e.TarifName LIKE '%NATRIUM%'))
    OR (p.PanelCode = 'IMUNOSEROLOGI' AND (e.TarifName LIKE '%HBSAG%' OR e.TarifName LIKE '%HIV%' OR e.TarifName LIKE '%WIDAL%'))
    OR (p.PanelCode = 'HEMOSTASIS'    AND (e.TarifName LIKE '%HEMOSTASIS%' OR e.TarifName LIKE '%APTT%' OR e.TarifName LIKE '%PT %'))
GROUP BY p.PanelCode, p.PanelName
ORDER BY p.PanelCode;

-- ==============================================================================================
-- 8. AUDIT REPORT SECTION 6: DETAILED MASTER ALIGNMENT TABLE
-- ==============================================================================================
SELECT 
    e.TarifId,
    e.TarifName,
    ISNULL(e.TestDefinitionId, '-') AS TestDefinitionId,
    ISNULL(e.LabTestCode, '-') AS LabTestCode,
    ISNULL(e.LabTestName, '-') AS LabTestName,
    ISNULL(e.SpecimenType, '-') AS SpecimenType,
    e.VacutainerName,
    ISNULL(CONVERT(VARCHAR(5), e.DefinitionIsActive), '-') AS IsActive,
    e.TotalComponents,
    e.ActiveMasterComponents,
    e.OverallStatus,
    e.PotentialRuntimeError
FROM #DefinitionEvaluation e
WHERE (@OnlyDefects = 0 OR e.OverallStatus != 'ALIGNED_READY')
ORDER BY 
    CASE e.OverallStatus
        WHEN 'UNMAPPED' THEN 1
        WHEN 'EMPTY_COMPONENTS' THEN 2
        WHEN 'INVALID_MLC_REFERENCE' THEN 3
        WHEN 'INACTIVE_MLC_REFERENCE' THEN 4
        WHEN 'STRUCTURAL_DEFECT' THEN 5
        WHEN 'INACTIVE_DRAFT' THEN 6
        ELSE 7
    END,
    e.TarifId;

-- ==============================================================================================
-- 9. REMEDIATION SQL SNIPPET TEMPLATE (FOR UNMAPPED / DRAFT TARIFS)
-- ==============================================================================================
PRINT '==============================================================================================';
PRINT 'PANDUAN REMEDIASI TARIF UNMAPPED / INAKTIF:';
PRINT '1. Jika tarif baru ditambahkan di master tarif (ta_tarif) dan digunakan pada pencarian EMR,';
PRINT '   buat baris pada BILRG_LabTestDefinition dan komponennya di BILRG_LabTestComponent:';
PRINT '';
PRINT '   -- Contoh Template Penambahan Definisi Uji Baru:';
PRINT '   INSERT INTO BILRG_LabTestDefinition (';
PRINT '       TestDefinitionId, TarifId, TarifCode, TarifName, LabTestCode, LabTestName,';
PRINT '       SpecimenType, VacutainerType, IsActive, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate';
PRINT '   ) VALUES (';
PRINT '       ''LTD00xx'', ''LABxxx'', ''LABxxx'', ''Nama Pemeriksaan'', ''LABxxx'', ''Nama Pemeriksaan'',';
PRINT '       ''Darah'', 2, 1, ''ADMIN_LAB'', GETDATE(), ''ADMIN_LAB'', GETDATE(), '''', ''3000-01-01''';
PRINT '   );';
PRINT '';
PRINT '   -- Tambahkan Komponen Hasil (Mengacu ke BILRG_LabComponentMaster - MLCxxxx):';
PRINT '   INSERT INTO BILRG_LabTestComponent (';
PRINT '       TestDefinitionId, SequenceNo, ComponentId, ReferenceRangeOverride, RequiredFlagging, IsMandatory';
PRINT '   ) VALUES (';
PRINT '       ''LTD00xx'', 1, ''MLC0001'', '''', 0, 1';
PRINT '   );';
PRINT '==============================================================================================';
