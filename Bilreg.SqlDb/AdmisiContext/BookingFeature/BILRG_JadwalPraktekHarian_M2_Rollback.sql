IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BILRG_JPH_Tgl_Dokter_Jam')
    DROP INDEX UX_BILRG_JPH_Tgl_Dokter_Jam ON BILRG_JadwalPraktekHarian;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_JPH_JadwalPraktekId_Tgl')
    DROP INDEX IX_BILRG_JPH_JadwalPraktekId_Tgl ON BILRG_JadwalPraktekHarian;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BILRG_JPH_TglPraktek')
    DROP INDEX IX_BILRG_JPH_TglPraktek ON BILRG_JadwalPraktekHarian;
GO

IF OBJECT_ID('BILRG_JadwalPraktekHarian', 'U') IS NOT NULL
    DROP TABLE BILRG_JadwalPraktekHarian;
GO
