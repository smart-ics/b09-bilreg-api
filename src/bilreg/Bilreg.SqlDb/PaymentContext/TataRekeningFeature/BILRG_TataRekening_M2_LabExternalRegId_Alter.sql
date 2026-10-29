-- BILRG_TataRekening_M2_LabExternalRegId_Alter.sql
-- Migration M2: Widen RegId to VARCHAR(15) to support Lab External Registration IDs (e.g. LEX...)

IF COL_LENGTH('BILRG_TataRekening', 'RegId') IS NOT NULL
   AND COL_LENGTH('BILRG_TataRekening', 'RegId') < 15
BEGIN
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @pkName NVARCHAR(128);
    DECLARE @dfName NVARCHAR(128);

    -- Drop PK constraint if present
    IF EXISTS (
        SELECT 1 FROM sys.key_constraints 
        WHERE name = 'PK_BILRG_TataRekening' AND parent_object_id = OBJECT_ID('BILRG_TataRekening')
    )
    BEGIN
        ALTER TABLE BILRG_TataRekening DROP CONSTRAINT PK_BILRG_TataRekening;
    END

    -- Also drop any system-named PK constraint on BILRG_TataRekening if present
    SELECT @pkName = name 
    FROM sys.key_constraints 
    WHERE parent_object_id = OBJECT_ID('BILRG_TataRekening') AND [type] = 'PK';
    IF @pkName IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE BILRG_TataRekening DROP CONSTRAINT ' + QUOTENAME(@pkName);
        EXEC sys.sp_executesql @sql;
    END

    -- Drop Default constraint if present
    IF EXISTS (
        SELECT 1 FROM sys.default_constraints 
        WHERE name = 'DF_BILRG_TataRekening_RegId' AND parent_object_id = OBJECT_ID('BILRG_TataRekening')
    )
    BEGIN
        ALTER TABLE BILRG_TataRekening DROP CONSTRAINT DF_BILRG_TataRekening_RegId;
    END

    -- Also drop any system-named default constraint on RegId column if present
    SELECT @dfName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('BILRG_TataRekening') AND c.name = 'RegId';
    IF @dfName IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE BILRG_TataRekening DROP CONSTRAINT ' + QUOTENAME(@dfName);
        EXEC sys.sp_executesql @sql;
    END

    -- Alter column to VARCHAR(15) NOT NULL
    ALTER TABLE BILRG_TataRekening ALTER COLUMN RegId VARCHAR(15) NOT NULL;

    -- Add back default constraint
    IF NOT EXISTS (
        SELECT 1 FROM sys.default_constraints 
        WHERE name = 'DF_BILRG_TataRekening_RegId' AND parent_object_id = OBJECT_ID('BILRG_TataRekening')
    )
    BEGIN
        ALTER TABLE BILRG_TataRekening ADD CONSTRAINT DF_BILRG_TataRekening_RegId DEFAULT('') FOR RegId;
    END

    -- Add back primary key constraint
    IF NOT EXISTS (
        SELECT 1 FROM sys.key_constraints 
        WHERE name = 'PK_BILRG_TataRekening' AND parent_object_id = OBJECT_ID('BILRG_TataRekening')
    )
    BEGIN
        ALTER TABLE BILRG_TataRekening ADD CONSTRAINT PK_BILRG_TataRekening PRIMARY KEY CLUSTERED (RegId);
    END
END
GO
