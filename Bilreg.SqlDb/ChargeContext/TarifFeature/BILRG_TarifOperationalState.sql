IF OBJECT_ID('BILRG_TarifOperationalState', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_TarifOperationalState
    (
        RowId                 INT NOT NULL CONSTRAINT DF_BILRG_TarifOperationalState_RowId DEFAULT(1),
        MigrationMode         INT NULL,
        LastImportAt          DATETIME NULL,
        LastImportBy          VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TarifOperationalState_LastImportBy DEFAULT(''),
        LastBaselineAt        DATETIME NULL,
        LastBaselinePolicyId  VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_TarifOperationalState_LastBaselinePolicyId DEFAULT(''),
        UpdatedAt             DATETIME NOT NULL CONSTRAINT DF_BILRG_TarifOperationalState_UpdatedAt DEFAULT('3000-01-01'),
        UpdatedBy             VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TarifOperationalState_UpdatedBy DEFAULT(''),
        CONSTRAINT PK_BILRG_TarifOperationalState PRIMARY KEY CLUSTERED (RowId),
        CONSTRAINT CK_BILRG_TarifOperationalState_SingleRow CHECK (RowId = 1)
    );

    INSERT INTO BILRG_TarifOperationalState (RowId, UpdatedAt, UpdatedBy)
    VALUES (1, GETDATE(), 'SYSTEM');
END
