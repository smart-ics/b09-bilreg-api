-- BILRG_AdmissionQueue_Rollback.sql
-- DESTRUCTIVE: drops Admission Queue additive tables created by the V1 migration manifest.
-- Use ONLY on disposable / integration databases when no operational data must be retained.
-- After full backup. Never run against production with retained queue/session data.
--
-- Brownfield / production with data: disable AdmissionQueueApi feature flags and redeploy
-- the previous Bilreg.Api binary. Do NOT DROP TABLE.
--
-- Does NOT drop: physician/legacy ta_* maps, BILRG_PasienTracker*, unrelated schemas.
-- Reverse order of create tables (alters go away with table drops).

SET NOCOUNT ON;

-- BILRG_AdmKioskServicePoint / BILRG_AdmQueueKiosk
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AdmKioskServicePoint_ServicePointId'
           AND object_id = OBJECT_ID('BILRG_AdmKioskServicePoint'))
    DROP INDEX IX_BILRG_AdmKioskServicePoint_ServicePointId ON BILRG_AdmKioskServicePoint;
GO

IF OBJECT_ID('BILRG_AdmKioskServicePoint', 'U') IS NOT NULL
    DROP TABLE BILRG_AdmKioskServicePoint;
GO

IF OBJECT_ID('BILRG_AdmQueueKiosk', 'U') IS NOT NULL
    DROP TABLE BILRG_AdmQueueKiosk;
GO

-- BILRG_AdmBookingAssistance
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AdmBookingAssistance_Active'
           AND object_id = OBJECT_ID('BILRG_AdmBookingAssistance'))
    DROP INDEX IX_BILRG_AdmBookingAssistance_Active ON BILRG_AdmBookingAssistance;
GO

IF OBJECT_ID('BILRG_AdmBookingAssistance', 'U') IS NOT NULL
    DROP TABLE BILRG_AdmBookingAssistance;
GO

-- BILRG_RegOutcome
IF OBJECT_ID('BILRG_RegOutcome', 'U') IS NOT NULL
    DROP TABLE BILRG_RegOutcome;
GO

-- BILRG_AdmLoketCurrentCall
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AdmLoketCurrentCall_ActiveDisplay'
           AND object_id = OBJECT_ID('BILRG_AdmLoketCurrentCall'))
    DROP INDEX IX_BILRG_AdmLoketCurrentCall_ActiveDisplay ON BILRG_AdmLoketCurrentCall;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BILRG_AdmLoketCurrentCall_ActiveEntry'
           AND object_id = OBJECT_ID('BILRG_AdmLoketCurrentCall'))
    DROP INDEX UX_BILRG_AdmLoketCurrentCall_ActiveEntry ON BILRG_AdmLoketCurrentCall;
GO

IF OBJECT_ID('BILRG_AdmLoketCurrentCall', 'U') IS NOT NULL
    DROP TABLE BILRG_AdmLoketCurrentCall;
GO

-- BILRG_AdmServicePoint
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BILRG_AdmServicePoint_ActivePrefix'
           AND object_id = OBJECT_ID('BILRG_AdmServicePoint'))
    DROP INDEX UX_BILRG_AdmServicePoint_ActivePrefix ON BILRG_AdmServicePoint;
GO

IF OBJECT_ID('BILRG_AdmServicePoint', 'U') IS NOT NULL
    DROP TABLE BILRG_AdmServicePoint;
GO

-- BILRG_AntrianEntry (includes M2 queue-ops + M3 audit columns)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_AntrianEntry_OperationalWorklist'
           AND object_id = OBJECT_ID('BILRG_AntrianEntry'))
    DROP INDEX IX_BILRG_AntrianEntry_OperationalWorklist ON BILRG_AntrianEntry;
GO

IF OBJECT_ID('BILRG_AntrianEntry', 'U') IS NOT NULL
    DROP TABLE BILRG_AntrianEntry;
GO

-- BILRG_Antrian (includes M1 SequenceTag / M2 prefix snapshot / M3 audit columns)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BILRG_Antrian_SequenceTag'
           AND object_id = OBJECT_ID('BILRG_Antrian'))
    DROP INDEX UX_BILRG_Antrian_SequenceTag ON BILRG_Antrian;
GO

IF OBJECT_ID('BILRG_Antrian', 'U') IS NOT NULL
    DROP TABLE BILRG_Antrian;
GO
