/* ==============================================================================================
   SCRIPT: ImportLegacyLabTestDefinition.sql
   CONTEXT: LabContext / LabTestDefinitionFeature
   TARGET DATABASE: HOSPITAL_PKL (server: dev.smart-ics.com)
   
   PURPOSE:
   Import & generate Lab Test Definitions (BILRG_LabTestDefinition) and their Component Sets
   (BILRG_LabTestComponent) from legacy billing & lab master tables:
     - Source: TA_TARIF, TA_TARIF6, TA_JENIS_PEMERIKSAAN, BIPUB_MapLabLoinc
     - Reference Master: BILRG_LabComponentMaster (169 standard vendor master items)
     - Target: BILRG_LabTestDefinition, BILRG_LabTestComponent
   
   SPECIFICATION RULES (LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md):
     - TestDefinitionId format: 'LTD' + 4 uppercase hex (e.g. LTD0001, LTD0002...)
     - Invariants: One active LabTestDefinition per TarifId; >= 1 component when active.
     - Strict Master rule: ComponentId MUST reference active BILRG_LabComponentMaster (MLCxxxx).
     - Completeness rule: If 100% of Tarif components are mapped -> IsActive = 1.
                          If any component is unmapped / missing -> IsActive = 0 (draft / gap).
     - VacutainerType: 0 = None/Pot, 1 = EDTA (Purple), 2 = Serum (Yellow/Red), 3 = Citrate (Blue).
     - SpecimenType: 'Darah' / 'Urine' / 'Feces' / 'Sputum' derived from test domain.
     - IsMandatory: 1 (required for result entry).
     - Execution Mode: Supports @DryRun = 1 (preview / rollback) and @DryRun = 0 (commit).
============================================================================================== */

SET NOCOUNT ON;

-- PARAMETERS
DECLARE @DryRun  BIT         = 1;            -- 1 = Dry Run (Rollback & Report), 0 = Commit Live Data
DECLARE @CrtUser VARCHAR(50) = 'MIGRATION';  -- Audit user
DECLARE @Now     DATETIME    = GETDATE();

BEGIN TRANSACTION;

BEGIN TRY
    -- -----------------------------------------------------------------------------------------
    -- STEP 1: BUILT-IN CLINICAL ALIAS & SYNONYM DICTIONARY
    -- -----------------------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#ComponentAlias') IS NOT NULL DROP TABLE #ComponentAlias;
    CREATE TABLE #ComponentAlias (
        LegacyCode        VARCHAR(20) NOT NULL PRIMARY KEY,
        TargetComponentId VARCHAR(7)  NOT NULL,
        Notes             VARCHAR(100)
    );

    INSERT INTO #ComponentAlias (LegacyCode, TargetComponentId, Notes) VALUES
    -- Hematologi Rutin & Lengkap
    ('LG001', 'MLC0001', 'Hemoglobin / Hb'),
    ('LG002', 'MLC0002', 'Leukosit / WBC'),
    ('LG006', 'MLC0003', 'Eritrosit / RBC'),
    ('LG008', 'MLC0004', 'Trombosit / PLT'),
    ('171',   'MLC0004', 'Thrombosit'),
    ('LG007', 'MLC000D', 'Hematokrit / HCT'),
    ('LG017', 'MLC000E', 'MCV'),
    ('LG018', 'MLC000F', 'MCH'),
    ('LG019', 'MLC0010', 'MCHC'),
    ('LG009', 'MLC001D', 'Retikulosit'),
    ('LG003', 'MLC001C', 'LED 1 Jam / ESR'),
    ('LG004', 'MLC001C', 'LED 2 Jam / ESR'),
    ('1013',  'MLC001C', 'LED 1 Jam'),
    ('1014',  'MLC001C', 'LED 2 Jam'),
    ('601',   'MLC001C', 'Laju Endap Darah 2'),
    -- Diff Count (Hitung Jenis Leukosit)
    ('LG010', 'MLC0016', 'Basofil %'),
    ('LG011', 'MLC0015', 'Eosinofil %'),
    ('LG012', 'MLC0025', 'Band / Stab %'),
    ('LG013', 'MLC0012', 'Segmen / Neutrofil %'),
    ('LG014', 'MLC0013', 'Limfosit %'),
    ('LG015', 'MLC0014', 'Monosit %'),
    -- Kimia Darah - Fungsi Hati & Ginjal
    ('LA006', 'MLC0009', 'SGOT / AST'),
    ('LA007', 'MLC0008', 'SGPT / ALT'),
    ('LA011', 'MLC0006', 'BUN / Urea'),
    ('LA012', 'MLC0006', 'Ureum / Urea'),
    ('LA010', 'MLC0007', 'Creatinin / Creatinine'),
    ('LA001', 'MLC002E', 'Total Protein'),
    ('LA002', 'MLC002D', 'Albumin'),
    ('LA003', 'MLC002F', 'Globulin'),
    ('LA004', 'MLC0027', 'Bilirubin Total'),
    ('LA005', 'MLC0028', 'Bilirubin Direk'),
    ('LA008', 'MLC002A', 'Alkali Phosphatase / ALP'),
    ('LA009', 'MLC0033', 'Uric Acid / Asam Urat'),
    -- Profil Lipid
    ('LB001', 'MLC000A', 'Cholesterol Total'),
    ('LB002', 'MLC004F', 'Triglycerid'),
    ('LDL2',  'MLC004F', 'Trigliserida'),
    ('LB003', 'MLC004E', 'Cholesterol LDL'),
    ('LDL',   'MLC004E', 'LDL'),
    ('LB004', 'MLC004D', 'Cholesterol HDL'),
    -- Glukosa & Diabetes
    ('LD001', 'MLC0005', 'Glukosa Puasa'),
    ('LD002', 'MLC0005', 'Glukosa 2 Jam PP'),
    ('LD003', 'MLC0005', 'Glukosa Sewaktu'),
    ('505',   'MLC0005', 'Glukosa Darah Sewaktu'),
    ('LZ010', 'MLC0037', 'HbA1c'),
    -- Elektrolit & Mineral
    ('LC001', 'MLC0034', 'Calcium'),
    ('LC002', 'MLC0056', 'Chlorides / Chloride'),
    ('LZ007', 'MLC0036', 'Magnesium'),
    ('392',   'MLC0054', 'Natrium Urin'),
    ('393',   'MLC0055', 'Kalium Urin'),
    -- Urinalisis Lengkap & Sedimen
    ('LO001', 'MLC009D', 'Warna Urin'),
    ('LO004', 'MLC0094', 'Berat Jenis Urin'),
    ('LO007', 'MLC000B', 'pH Urin'),
    ('LO008', 'MLC008E', 'Protein Urin'),
    ('LO009', 'MLC008F', 'Glukosa Urin'),
    ('LO010', 'MLC0090', 'Keton Urin'),
    ('LO012', 'MLC0096', 'Bilirubin Urin'),
    ('LO011', 'MLC0095', 'Urobilinogen Urin'),
    ('LO006', 'MLC0092', 'Nitrit Urin'),
    ('LO005', 'MLC0093', 'Esterase Leukosit Urin'),
    ('LO013', 'MLC0091', 'Blood Urin'),
    ('LO014', 'MLC0099', 'Sedimen Eritrosit Urin'),
    ('LO015', 'MLC009A', 'Sedimen Leukosit Urin'),
    ('LO016', 'MLC009B', 'Sedimen Epitel Urin'),
    ('LO017', 'MLC0097', 'Sedimen Silinder Urin'),
    ('LO018', 'MLC0098', 'Sedimen Kristal Urin'),
    -- Imunoserologi & Penyakit Menular
    ('LM002', 'MLC0069', 'HBsAg'),
    ('LM003', 'MLC006A', 'Anti HBs'),
    ('HIV',   'MLC0070', 'Anti HIV'),
    ('LM001', 'MLC0073', 'VDRL'),
    ('LZ011', 'MLC0088', 'CRP Kualitatif'),
    ('LZ012', 'MLC007C', 'Rheumatoid Factor / RF'),
    ('LZ013', 'MLC007D', 'ASTO Screening / ASO'),
    ('LZ014', 'MLC007D', 'ASRO Titer / ASO'),
    ('LP001', 'MLC004A', 'Test Kehamilan / Beta HCG'),
    ('LK001', 'MLC007A', 'Widal S. Typhi O'),
    ('LK002', 'MLC007B', 'Widal S. Typhi H'),
    ('LL001', 'MLC0078', 'Salmonella Typhi IgM / Typhidot'),
    ('LZ004', 'MLC00A3', 'BTA'),
    ('LZ008', 'MLC0043', 'Creatine Kinase / CK'),
    ('LZ009', 'MLC0044', 'CKMB'),
    ('LE001', 'MLC003B', 'Iron Serum'),
    ('LE002', 'MLC003C', 'TIBC'),
    ('535',   'MLC0022', 'Golongan Darah ABO'),
    ('LG020', 'MLC0042', 'Bleeding Time / BT'),
    ('LG021', 'MLC0043', 'Clotting Time / CT'),
    ('537',   'MLC0042', 'Masa Pendarahan BT'),
    ('LG023', 'MLC0087', 'Malaria Rapid Test');

    -- -----------------------------------------------------------------------------------------
    -- STEP 2: RESOLVE COMPONENT MAPPING FOR ALL LAB TARIFS IN TA_TARIF6
    -- -----------------------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#ResolvedComponents') IS NOT NULL DROP TABLE #ResolvedComponents;
    CREATE TABLE #ResolvedComponents (
        TarifId                VARCHAR(12)  NOT NULL,
        TarifName              VARCHAR(100) NOT NULL,
        SequenceNo             INT          NOT NULL,
        LegacyComponentCode    VARCHAR(20)  NOT NULL,
        LegacyComponentName    VARCHAR(100) NOT NULL,
        ResolvedComponentId    VARCHAR(7)   NULL,
        ResolvedComponentName  VARCHAR(120) NULL,
        MatchMethod            VARCHAR(30)  NULL
    );

    INSERT INTO #ResolvedComponents (
        TarifId, TarifName, SequenceNo, LegacyComponentCode, LegacyComponentName,
        ResolvedComponentId, ResolvedComponentName, MatchMethod
    )
    SELECT 
        t6.fs_kd_tarif,
        t.fs_nm_tarif,
        t6.fn_no_urut,
        jp.fs_kd_jenis_pemeriksaan,
        jp.fs_nm_jenis_pemeriksaan,
        COALESCE(ca.TargetComponentId, cm_exact_en.ComponentId, cm_exact_id.ComponentId, cm_loinc.ComponentId),
        COALESCE(cm_alias.ComponentName, cm_exact_en.ComponentName, cm_exact_id.ComponentName, cm_loinc.ComponentName),
        CASE 
            WHEN ca.TargetComponentId IS NOT NULL THEN 'ALIAS_DICTIONARY'
            WHEN cm_exact_en.ComponentId IS NOT NULL THEN 'EXACT_NAME_EN'
            WHEN cm_exact_id.ComponentId IS NOT NULL THEN 'EXACT_NAME_ID'
            WHEN cm_loinc.ComponentId IS NOT NULL THEN 'LOINC_MAPPING'
            ELSE 'UNMAPPED'
        END
    FROM TA_TARIF6 t6
    JOIN TA_TARIF t ON t6.fs_kd_tarif = t.fs_kd_tarif
    JOIN TA_JENIS_PEMERIKSAAN jp ON t6.fs_kd_jenis_pemeriksaan = jp.fs_kd_jenis_pemeriksaan
    LEFT JOIN #ComponentAlias ca ON jp.fs_kd_jenis_pemeriksaan = ca.LegacyCode
    LEFT JOIN BILRG_LabComponentMaster cm_alias ON ca.TargetComponentId = cm_alias.ComponentId
    LEFT JOIN BILRG_LabComponentMaster cm_exact_en 
        ON UPPER(RTRIM(LTRIM(jp.fs_nm_jenis_pemeriksaan))) = UPPER(RTRIM(LTRIM(cm_exact_en.ComponentName)))
    LEFT JOIN BILRG_LabComponentMaster cm_exact_id 
        ON UPPER(RTRIM(LTRIM(jp.fs_nm_jenis_pemeriksaan))) = UPPER(RTRIM(LTRIM(cm_exact_id.ComponentNameIndonesia)))
    LEFT JOIN BIPUB_MapLabLoinc ml ON jp.fs_kd_jenis_pemeriksaan = ml.PemeriksaanId
    LEFT JOIN BILRG_LabComponentMaster cm_loinc ON ml.LoincId = cm_loinc.LoincCode
    WHERE t6.fs_kd_tarif LIKE 'LAB%'
      -- Exclude legacy group header rows that are not analytes
      AND jp.fs_kd_jenis_pemeriksaan NOT IN ('LG005'); -- 'HITUNG JENIS (DIFF.)' header

    -- -----------------------------------------------------------------------------------------
    -- STEP 3: RESOLVE LAB TEST DEFINITION AGGREGATES & SEQUENTIAL LTD ID
    -- -----------------------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#CandidateDefinitions') IS NOT NULL DROP TABLE #CandidateDefinitions;
    CREATE TABLE #CandidateDefinitions (
        TarifId            VARCHAR(12)  NOT NULL,
        TarifName          VARCHAR(100) NOT NULL,
        TotalComponents    INT          NOT NULL,
        MappedComponents   INT          NOT NULL,
        IsActive           BIT          NOT NULL,
        SpecimenType       VARCHAR(30)  NOT NULL,
        VacutainerType     INT          NOT NULL,
        ExistingLtdId      VARCHAR(7)   NULL,
        NewRowIndex        INT          NULL,
        FinalLtdId         VARCHAR(7)   NULL
    );

    INSERT INTO #CandidateDefinitions (
        TarifId, TarifName, TotalComponents, MappedComponents, IsActive, SpecimenType, VacutainerType, ExistingLtdId
    )
    SELECT 
        c.TarifId,
        c.TarifName,
        COUNT(*) AS TotalComponents,
        COUNT(c.ResolvedComponentId) AS MappedComponents,
        CASE 
            WHEN COUNT(*) = COUNT(c.ResolvedComponentId) AND COUNT(*) > 0 THEN 1 
            ELSE 0 
        END AS IsActive,
        -- Determine Specimen Type
        CASE 
            WHEN c.TarifName LIKE '%URIN%' THEN 'Urine'
            WHEN c.TarifName LIKE '%FESES%' OR c.TarifName LIKE '%TINJA%' THEN 'Feces'
            WHEN c.TarifName LIKE '%SPUTUM%' OR c.TarifName LIKE '%DAHAK%' THEN 'Sputum'
            WHEN c.TarifName LIKE '%LIQUOR%' OR c.TarifName LIKE '%LCS%' THEN 'CSF'
            ELSE 'Darah'
        END AS SpecimenType,
        -- Determine Vacutainer Type (Valid VacutainerTypeEnum: 1=EDTA, 2=Serum, 3=Citrate, 4=Heparin)
        CASE 
            WHEN c.TarifName LIKE '%HEMOSTASIS%' OR c.TarifName LIKE '%APTT%' OR c.TarifName LIKE '%FIBRIN%'
              OR c.TarifName LIKE '% PT' OR c.TarifName LIKE 'PT %' OR c.TarifName LIKE '% PT %' THEN 3
            WHEN c.TarifName LIKE '%DARAH%' OR c.TarifName LIKE '%HEMATO%' OR c.TarifName LIKE '%HB%' 
              OR c.TarifName LIKE '%LEUKO%' OR c.TarifName LIKE '%LED%' OR c.TarifName LIKE '%GOLONGAN%' 
              OR c.TarifName LIKE '%DIFF%' THEN 1
            ELSE 2 -- Serum (Default for Chemistry, Serology, Hormones, etc. Must be in 1..4)
        END AS VacutainerType,
        ltd_exist.TestDefinitionId AS ExistingLtdId
    FROM #ResolvedComponents c
    OUTER APPLY (
        SELECT TOP 1 TestDefinitionId 
        FROM BILRG_LabTestDefinition 
        WHERE TarifId = c.TarifId
        ORDER BY TestDefinitionId
    ) ltd_exist
    GROUP BY c.TarifId, c.TarifName, ltd_exist.TestDefinitionId;

    -- Calculate next sequential LTD ID suffix for new rows
    DECLARE @MaxExistingLtdSuffix INT = 0;
    SELECT @MaxExistingLtdSuffix = ISNULL(MAX(CONVERT(INT, CONVERT(VARBINARY(4), '0x' + SUBSTRING(TestDefinitionId, 4, 4), 1))), 0)
    FROM BILRG_LabTestDefinition
    WHERE TestDefinitionId LIKE 'LTD[0-9A-F][0-9A-F][0-9A-F][0-9A-F]';

    -- Assign row numbers for definitions that don't have existing TestDefinitionId
    ;WITH NumberedNew AS (
        SELECT TarifId, ROW_NUMBER() OVER (ORDER BY TarifId) AS RowNum
        FROM #CandidateDefinitions
        WHERE ExistingLtdId IS NULL
    )
    UPDATE cd
    SET NewRowIndex = nn.RowNum,
        FinalLtdId = 'LTD' + FORMAT(@MaxExistingLtdSuffix + nn.RowNum, 'X4')
    FROM #CandidateDefinitions cd
    JOIN NumberedNew nn ON cd.TarifId = nn.TarifId;

    -- Retain existing TestDefinitionId for existing definitions
    UPDATE #CandidateDefinitions
    SET FinalLtdId = ExistingLtdId
    WHERE ExistingLtdId IS NULL AND FinalLtdId IS NULL OR ExistingLtdId IS NOT NULL;

    -- -----------------------------------------------------------------------------------------
    -- STEP 4: UPSERT BILRG_LabTestDefinition
    -- -----------------------------------------------------------------------------------------
    -- 4a. Update existing definitions
    UPDATE def
    SET 
        def.TarifCode      = cd.TarifId,
        def.TarifName      = cd.TarifName,
        def.LabTestCode    = cd.TarifId,
        def.LabTestName    = cd.TarifName,
        def.SpecimenType   = cd.SpecimenType,
        def.VacutainerType = cd.VacutainerType,
        def.IsActive       = cd.IsActive,
        def.UpdUser        = @CrtUser,
        def.UpdDate        = @Now
    FROM BILRG_LabTestDefinition def
    JOIN #CandidateDefinitions cd ON def.TestDefinitionId = cd.FinalLtdId;

    -- 4b. Insert new definitions
    INSERT INTO BILRG_LabTestDefinition (
        TestDefinitionId, TarifId, TarifCode, TarifName, LabTestCode, LabTestName,
        SpecimenType, VacutainerType, IsActive,
        CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
    )
    SELECT 
        cd.FinalLtdId,
        cd.TarifId,
        cd.TarifId,
        cd.TarifName,
        cd.TarifId,
        cd.TarifName,
        cd.SpecimenType,
        cd.VacutainerType,
        cd.IsActive,
        @CrtUser, @Now, @CrtUser, @Now, '', '3000-01-01'
    FROM #CandidateDefinitions cd
    WHERE NOT EXISTS (
        SELECT 1 FROM BILRG_LabTestDefinition WHERE TestDefinitionId = cd.FinalLtdId
    );

    -- -----------------------------------------------------------------------------------------
    -- STEP 5: UPSERT BILRG_LabTestComponent
    -- -----------------------------------------------------------------------------------------
    -- 5a. Remove existing child components for the affected definitions
    DELETE tc
    FROM BILRG_LabTestComponent tc
    JOIN #CandidateDefinitions cd ON tc.TestDefinitionId = cd.FinalLtdId;

    -- 5b. Insert resolved mapped components
    INSERT INTO BILRG_LabTestComponent (
        TestDefinitionId, SequenceNo, ComponentId, ReferenceRangeOverride, RequiredFlagging, IsMandatory
    )
    SELECT 
        cd.FinalLtdId,
        ROW_NUMBER() OVER (PARTITION BY cd.FinalLtdId ORDER BY rc.SequenceNo) AS SequenceNo,
        rc.ResolvedComponentId,
        '' AS ReferenceRangeOverride,
        0  AS RequiredFlagging,
        1  AS IsMandatory
    FROM #ResolvedComponents rc
    JOIN #CandidateDefinitions cd ON rc.TarifId = cd.TarifId
    WHERE rc.ResolvedComponentId IS NOT NULL;

    -- -----------------------------------------------------------------------------------------
    -- STEP 6: PRODUCE AUDIT & STATUS REPORTS
    -- -----------------------------------------------------------------------------------------
    
    -- REPORT 1: SUMMARY STATISTICS
    SELECT 
        'MIGRATION_SUMMARY' AS ReportType,
        (SELECT COUNT(*) FROM #CandidateDefinitions) AS TotalLabTarifsEvaluated,
        (SELECT COUNT(*) FROM #CandidateDefinitions WHERE IsActive = 1) AS ActiveDefinitionsCount,
        (SELECT COUNT(*) FROM #CandidateDefinitions WHERE IsActive = 0) AS InactiveDraftDefinitionsCount,
        (SELECT COUNT(*) FROM #ResolvedComponents WHERE ResolvedComponentId IS NOT NULL) AS TotalMappedComponentLines,
        (SELECT COUNT(*) FROM #ResolvedComponents WHERE ResolvedComponentId IS NULL) AS TotalUnmappedComponentLines,
        CASE WHEN @DryRun = 1 THEN 'DRY_RUN (ROLLED_BACK)' ELSE 'LIVE (COMMITTED)' END AS ExecutionStatus;

    -- REPORT 2: GAP / UNMAPPED COMPONENTS REPORT
    SELECT 
        rc.TarifId,
        rc.TarifName,
        rc.SequenceNo,
        rc.LegacyComponentCode,
        rc.LegacyComponentName,
        'Item not in BILRG_LabComponentMaster' AS GapReason
    FROM #ResolvedComponents rc
    WHERE rc.ResolvedComponentId IS NULL
    ORDER BY rc.TarifId, rc.SequenceNo;

    -- REPORT 3: GENERATED LAB TEST DEFINITIONS SAMPLE / OVERVIEW
    SELECT 
        cd.FinalLtdId AS TestDefinitionId,
        cd.TarifId,
        cd.TarifName,
        cd.SpecimenType,
        cd.VacutainerType,
        cd.TotalComponents AS LegacyComponentsCount,
        cd.MappedComponents AS MappedComponentsCount,
        cd.IsActive
    FROM #CandidateDefinitions cd
    ORDER BY cd.IsActive DESC, cd.FinalLtdId;

    -- -----------------------------------------------------------------------------------------
    -- STEP 7: COMMIT OR ROLLBACK BASED ON @DryRun
    -- -----------------------------------------------------------------------------------------
    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'SUCCESS: Dry-run completed safely. All changes rolled back.';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'SUCCESS: Migration committed permanently to HOSPITAL_PKL.';
    END

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();

    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

