-- M10: Multi-version LabResultDocument + amendment metadata
-- Run after BILRG_LabResultDocument.sql (existing deployments).

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_LabResultDocument_OrderId'
      AND object_id = OBJECT_ID('dbo.BILRG_LabResultDocument', 'U'))
BEGIN
    DROP INDEX UX_BILRG_LabResultDocument_OrderId ON dbo.BILRG_LabResultDocument;
END
GO

IF COL_LENGTH('dbo.BILRG_LabResultDocument', 'AmendmentReason') IS NULL
BEGIN
    ALTER TABLE dbo.BILRG_LabResultDocument ADD
        AmendmentReason VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_AmendmentReason DEFAULT(''),
        AmendedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_AmendedDate DEFAULT('3000-01-01'),
        AmendedUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_AmendedUserId DEFAULT(''),
        PreviousVersionId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_PreviousVersionId DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_LabResultDocument_OrderId_VersionNo'
      AND object_id = OBJECT_ID('dbo.BILRG_LabResultDocument', 'U'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_BILRG_LabResultDocument_OrderId_VersionNo
        ON dbo.BILRG_LabResultDocument (OrderId, VersionNo)
        WHERE OrderId <> '';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_LabResultDocument_OrderId_Current'
      AND object_id = OBJECT_ID('dbo.BILRG_LabResultDocument', 'U'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_BILRG_LabResultDocument_OrderId_Current
        ON dbo.BILRG_LabResultDocument (OrderId)
        WHERE IsCurrentVersion = 1 AND OrderId <> '';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_LabResultDocument_OrderId_IsCurrent'
      AND object_id = OBJECT_ID('dbo.BILRG_LabResultDocument', 'U'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_LabResultDocument_OrderId_IsCurrent
        ON dbo.BILRG_LabResultDocument (OrderId, IsCurrentVersion);
END
GO
