-- BILRG_LabTestDefinition_Discipline_Mapping.sql
-- Mapping of BILRG_LabTestDefinition to canonical BILRG_LabDiscipline and LaboratoryServiceType

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '>>> [MAPPING] Updating DisciplineId and ServiceType on BILRG_LabTestDefinition...';

-- 1. HEMATOLOGI (DISC-HEM, ServiceType = 1)
UPDATE BILRG_LabTestDefinition
SET DisciplineId = 'DISC-HEM', ServiceType = 1
WHERE (
    TarifCode LIKE 'LAB0103%'
    OR TarifCode IN ('LAB0116003', 'LAB0116034', 'LAB0221005', 'LAB0221006', 'LAB0221009')
    OR TestDefinitionId = 'LTD0005'
);

-- 2. KIMIA KLINIK (DISC-KIM, ServiceType = 1)
UPDATE BILRG_LabTestDefinition
SET DisciplineId = 'DISC-KIM', ServiceType = 1
WHERE (
    TarifCode LIKE 'LAB0101%'
    OR TarifCode LIKE 'LAB0102%'
    OR TarifCode IN ('LAB0201001', 'LAB0201002', 'LAB0201003', 'LAB0203001', 'LAB0218040')
);

-- 3. IMUNOLOGI & SEROLOGI (DISC-IMM, ServiceType = 1)
UPDATE BILRG_LabTestDefinition
SET DisciplineId = 'DISC-IMM', ServiceType = 1
WHERE (
    TarifCode LIKE 'LAB0104%'
    OR TarifCode LIKE 'LAB0204%'
    OR TarifCode IN ('LAB0116036', 'LAB0211001', 'LAB0211003')
);

-- 4. URINALISIS & FESES (DISC-URI, ServiceType = 1)
UPDATE BILRG_LabTestDefinition
SET DisciplineId = 'DISC-URI', ServiceType = 1
WHERE (
    TarifCode LIKE 'LAB0108%'
    OR TarifCode IN ('LAB0109001', 'LAB0116005', 'LAB0116006')
);

-- 5. MIKROBIOLOGI KLINIK (DISC-MIK, ServiceType = 3, VacutainerType = 0)
UPDATE BILRG_LabTestDefinition
SET DisciplineId = 'DISC-MIK', ServiceType = 3, VacutainerType = 0, SpecimenType = 'Sekret Uretra/Vagina'
WHERE TarifCode = 'LAB0116002';

UPDATE BILRG_LabTestDefinition
SET DisciplineId = 'DISC-MIK', ServiceType = 3, VacutainerType = 0, SpecimenType = 'Sputum Dahak'
WHERE TarifCode = 'LAB0116004';

-- 6. PATOLOGI ANATOMI - HISTOPATOLOGI (DISC-PAH, ServiceType = 2, VacutainerType = 0)
UPDATE BILRG_LabTestDefinition
SET DisciplineId = 'DISC-PAH', ServiceType = 2, VacutainerType = 0, SpecimenType = 'Jaringan Biopsi'
WHERE TarifCode = 'LAB0221002';

-- 7. PATOLOGI ANATOMI - SITOLOGI (DISC-PAS, ServiceType = 2, VacutainerType = 0)
UPDATE BILRG_LabTestDefinition
SET DisciplineId = 'DISC-PAS', ServiceType = 2, VacutainerType = 0, SpecimenType = 'Swab Serviks'
WHERE TarifCode = 'LAB0216001';

UPDATE BILRG_LabTestDefinition
SET DisciplineId = 'DISC-PAS', ServiceType = 2, VacutainerType = 0, SpecimenType = 'Cairan Pleura'
WHERE TarifCode = 'LAB0221001';

-- 8. NON-BLOOD SPECIMENS (Faeces, Urinalisis non-vacutainer, dsb.)
UPDATE BILRG_LabTestDefinition
SET VacutainerType = 0, SpecimenType = 'Faeces'
WHERE TarifCode = 'LAB0109001';

PRINT '>>> [MAPPING] DisciplineId & ServiceType update complete.';
GO
