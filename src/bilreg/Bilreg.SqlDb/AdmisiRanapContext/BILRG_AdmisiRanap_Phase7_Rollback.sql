-- BILRG_AdmisiRanap_Phase7_Rollback.sql
-- DESTRUCTIVE: drops all AdmisiRanap Phase-2 tables and indexes.
-- Use only when no production data must be retained, or after full backup.
-- Reverse order: Waiting List -> Admission -> Reservation -> Opname Request

-- BILRG_BedWaitingList indexes
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_BedWaitingList_BangsalId_Status'
           AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
    DROP INDEX IX_BILRG_BedWaitingList_BangsalId_Status ON BILRG_BedWaitingList;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_BedWaitingList_Status_CrtDate'
           AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
    DROP INDEX IX_BILRG_BedWaitingList_Status_CrtDate ON BILRG_BedWaitingList;
GO

IF OBJECT_ID('BILRG_BedWaitingList', 'U') IS NOT NULL
    DROP TABLE BILRG_BedWaitingList;
GO

-- BILRG_AdmAdmission indexes
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AdmAdmission_PasienId_Status'
           AND object_id = OBJECT_ID('BILRG_AdmAdmission'))
    DROP INDEX IX_BILRG_AdmAdmission_PasienId_Status ON BILRG_AdmAdmission;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AdmAdmission_Status_CrtDate'
           AND object_id = OBJECT_ID('BILRG_AdmAdmission'))
    DROP INDEX IX_BILRG_AdmAdmission_Status_CrtDate ON BILRG_AdmAdmission;
GO

IF OBJECT_ID('BILRG_AdmAdmission', 'U') IS NOT NULL
    DROP TABLE BILRG_AdmAdmission;
GO

-- BILRG_AdmReservation indexes
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AdmReservation_Status_PlannedDate'
           AND object_id = OBJECT_ID('BILRG_AdmReservation'))
    DROP INDEX IX_BILRG_AdmReservation_Status_PlannedDate ON BILRG_AdmReservation;
GO

IF OBJECT_ID('BILRG_AdmReservation', 'U') IS NOT NULL
    DROP TABLE BILRG_AdmReservation;
GO

-- BILRG_AdmOpnameRequest indexes
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AdmOpnameRequest_Status_CrtDate'
           AND object_id = OBJECT_ID('BILRG_AdmOpnameRequest'))
    DROP INDEX IX_BILRG_AdmOpnameRequest_Status_CrtDate ON BILRG_AdmOpnameRequest;
GO

IF OBJECT_ID('BILRG_AdmOpnameRequest', 'U') IS NOT NULL
    DROP TABLE BILRG_AdmOpnameRequest;
GO
