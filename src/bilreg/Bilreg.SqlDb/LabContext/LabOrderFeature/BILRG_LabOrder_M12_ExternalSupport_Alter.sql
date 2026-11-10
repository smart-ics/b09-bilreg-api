-- BILRG_LabOrder_M12_ExternalSupport_Alter.sql
-- Migration M12: Support External Walk-In registration identifiers (RegId/ExecutionRegId VARCHAR(15), PatientId VARCHAR(20))

IF (COL_LENGTH('BILRG_LabOrder', 'RegId') IS NOT NULL AND COL_LENGTH('BILRG_LabOrder', 'RegId') < 15)
   OR (COL_LENGTH('BILRG_LabOrder', 'ExecutionRegId') IS NOT NULL AND COL_LENGTH('BILRG_LabOrder', 'ExecutionRegId') < 15)
   OR (COL_LENGTH('BILRG_LabOrder', 'PatientId') IS NOT NULL AND COL_LENGTH('BILRG_LabOrder', 'PatientId') < 20)
BEGIN
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @dfRegId NVARCHAR(128);
    DECLARE @dfExecRegId NVARCHAR(128);
    DECLARE @dfPatientId NVARCHAR(128);

    -- 1. Drop index IX_BILRG_LabOrder_Reg if present
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_BILRG_LabOrder_Reg' AND object_id = OBJECT_ID('BILRG_LabOrder')
    )
    BEGIN
        DROP INDEX IX_BILRG_LabOrder_Reg ON BILRG_LabOrder;
    END

    -- 2. Drop default constraints if present (by name and dynamic discovery)
    IF EXISTS (
        SELECT 1 FROM sys.default_constraints 
        WHERE name = 'DF_BILRG_LabOrder_RegId' AND parent_object_id = OBJECT_ID('BILRG_LabOrder')
    )
    BEGIN
        ALTER TABLE BILRG_LabOrder DROP CONSTRAINT DF_BILRG_LabOrder_RegId;
    END

    SELECT @dfRegId = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('BILRG_LabOrder') AND c.name = 'RegId';
    IF @dfRegId IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE BILRG_LabOrder DROP CONSTRAINT ' + QUOTENAME(@dfRegId);
        EXEC sys.sp_executesql @sql;
    END

    IF EXISTS (
        SELECT 1 FROM sys.default_constraints 
        WHERE name = 'DF_BILRG_LabOrder_ExecutionRegId' AND parent_object_id = OBJECT_ID('BILRG_LabOrder')
    )
    BEGIN
        ALTER TABLE BILRG_LabOrder DROP CONSTRAINT DF_BILRG_LabOrder_ExecutionRegId;
    END

    SELECT @dfExecRegId = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('BILRG_LabOrder') AND c.name = 'ExecutionRegId';
    IF @dfExecRegId IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE BILRG_LabOrder DROP CONSTRAINT ' + QUOTENAME(@dfExecRegId);
        EXEC sys.sp_executesql @sql;
    END

    IF EXISTS (
        SELECT 1 FROM sys.default_constraints 
        WHERE name = 'DF_BILRG_LabOrder_PatientId' AND parent_object_id = OBJECT_ID('BILRG_LabOrder')
    )
    BEGIN
        ALTER TABLE BILRG_LabOrder DROP CONSTRAINT DF_BILRG_LabOrder_PatientId;
    END

    SELECT @dfPatientId = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('BILRG_LabOrder') AND c.name = 'PatientId';
    IF @dfPatientId IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE BILRG_LabOrder DROP CONSTRAINT ' + QUOTENAME(@dfPatientId);
        EXEC sys.sp_executesql @sql;
    END

    -- 3. Alter columns
    ALTER TABLE BILRG_LabOrder ALTER COLUMN RegId VARCHAR(15) NOT NULL;
    ALTER TABLE BILRG_LabOrder ALTER COLUMN ExecutionRegId VARCHAR(15) NOT NULL;
    ALTER TABLE BILRG_LabOrder ALTER COLUMN PatientId VARCHAR(20) NOT NULL;

    -- 4. Add back default constraints
    IF NOT EXISTS (
        SELECT 1 FROM sys.default_constraints 
        WHERE name = 'DF_BILRG_LabOrder_RegId' AND parent_object_id = OBJECT_ID('BILRG_LabOrder')
    )
    BEGIN
        ALTER TABLE BILRG_LabOrder ADD CONSTRAINT DF_BILRG_LabOrder_RegId DEFAULT('') FOR RegId;
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.default_constraints 
        WHERE name = 'DF_BILRG_LabOrder_ExecutionRegId' AND parent_object_id = OBJECT_ID('BILRG_LabOrder')
    )
    BEGIN
        ALTER TABLE BILRG_LabOrder ADD CONSTRAINT DF_BILRG_LabOrder_ExecutionRegId DEFAULT('') FOR ExecutionRegId;
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.default_constraints 
        WHERE name = 'DF_BILRG_LabOrder_PatientId' AND parent_object_id = OBJECT_ID('BILRG_LabOrder')
    )
    BEGIN
        ALTER TABLE BILRG_LabOrder ADD CONSTRAINT DF_BILRG_LabOrder_PatientId DEFAULT('') FOR PatientId;
    END

    -- 5. Re-create index IX_BILRG_LabOrder_Reg
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_BILRG_LabOrder_Reg' AND object_id = OBJECT_ID('BILRG_LabOrder')
    )
    BEGIN
        CREATE INDEX IX_BILRG_LabOrder_Reg ON BILRG_LabOrder(RegId, LabOrderStatus) WHERE RegId <> '';
    END
END
GO
