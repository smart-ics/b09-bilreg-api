-- BILRG_LabResultDocument_M12_Alter.sql
-- Migration M12: Add ExpertiseNote and CriticalNote to BILRG_LabResultDocument

IF COL_LENGTH('BILRG_LabResultDocument', 'ExpertiseNote') IS NULL
BEGIN
    ALTER TABLE BILRG_LabResultDocument
        ADD ExpertiseNote VARCHAR(2000) NOT NULL
            CONSTRAINT DF_BILRG_LabResultDocument_ExpertiseNote DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_LabResultDocument', 'CriticalNote') IS NULL
BEGIN
    ALTER TABLE BILRG_LabResultDocument
        ADD CriticalNote VARCHAR(1000) NOT NULL
            CONSTRAINT DF_BILRG_LabResultDocument_CriticalNote DEFAULT('');
END
GO
