-- =====================================================================================
-- Migration Script: BILRG_LabTestComponent_ReferenceRange_Seed.sql
-- Purpose: Seed standard reference ranges for laboratory test components (analytes)
--          and backfill existing order components where ReferenceRangeText is empty.
-- Target DB: HOSPITAL_PKL / Production SIMRS MyHospital
-- =====================================================================================

IF OBJECT_ID('tempdb..#StdRef') IS NOT NULL DROP TABLE #StdRef;
CREATE TABLE #StdRef (
    ComponentCode VARCHAR(20) PRIMARY KEY,
    StandardRef   VARCHAR(100) NOT NULL
);

INSERT INTO #StdRef (ComponentCode, StandardRef) VALUES
('HB', 'L: 14.0 - 18.0, P: 12.0 - 16.0'),
('WBC', '4.0 - 10.0'),
('PLT', '150 - 450'),
('HCT', 'L: 40 - 52, P: 36 - 48'),
('RBC', 'L: 4.5 - 5.5, P: 4.0 - 5.0'),
('MCV', '80 - 100'),
('MCH', '26 - 34'),
('MCHC', '32 - 36'),
('ESR', 'L: 0 - 15, P: 0 - 20'),
('GLU', '70 - 140'),
('UREA', '15 - 45'),
('CREA', 'L: 0.7 - 1.3, P: 0.6 - 1.1'),
('UA', 'L: 3.5 - 7.2, P: 2.6 - 6.0'),
('CHOL', '< 200'),
('TRIG', '< 150'),
('HDL', '> 40'),
('LDL', '< 130'),
('AST', 'L: < 37, P: < 31'),
('ALT', 'L: < 42, P: < 32'),
('ALP', '30 - 120'),
('GGT', 'L: < 50, P: < 32'),
('BILI_T', '< 1.2'),
('BILI_D', '< 0.3'),
('ALB', '3.5 - 5.2'),
('TP', '6.4 - 8.3'),
('NA', '135 - 147'),
('K', '3.5 - 5.1'),
('CL', '96 - 106'),
('CA', '8.6 - 10.2'),
('HBA1C', '< 6.5'),
('HBSAG', 'NON REAKTIF'),
('ANTI_HBS', 'NON REAKTIF'),
('ANTI_HCV', 'NON REAKTIF'),
('ANTI_HIV', 'NON REAKTIF'),
('VDRL', 'NON REAKTIF'),
('TPHA', 'NON REAKTIF'),
('BHCG', 'Negatif'),
('COVID_AG', 'Negatif'),
('AFB_STN', 'Negatif');

-- 1. Update BILRG_LabTestComponent with StandardRef where ReferenceRangeOverride is empty
UPDATE tc
SET ReferenceRangeOverride = s.StandardRef
FROM BILRG_LabTestComponent tc
JOIN BILRG_LabComponentMaster cm ON tc.ComponentId = cm.ComponentId
JOIN #StdRef s ON cm.ComponentCode = s.ComponentCode
WHERE tc.ReferenceRangeOverride = '' OR tc.ReferenceRangeOverride IS NULL;

-- 2. Update existing orders in BILRG_LabOrderItemComponent where ReferenceRangeText is empty
UPDATE oic
SET ReferenceRangeText = s.StandardRef
FROM BILRG_LabOrderItemComponent oic
JOIN #StdRef s ON oic.ComponentCode = s.ComponentCode
WHERE oic.ReferenceRangeText = '' OR oic.ReferenceRangeText IS NULL;

IF OBJECT_ID('tempdb..#StdRef') IS NOT NULL DROP TABLE #StdRef;
