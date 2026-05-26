IF OBJECT_ID('BILRG_TarifPolicy', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_TarifPolicy
    (
        TarifPolicyId     VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_TarifPolicyId DEFAULT(''),
        PolicyNo          VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_PolicyNo DEFAULT(''),
        PolicyName        VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_PolicyName DEFAULT(''),
        EffectiveDateInfo DATETIME NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_EffectiveDateInfo DEFAULT('3000-01-01'),
        Description       VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_Description DEFAULT(''),
        PolicyStatus      INT NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_PolicyStatus DEFAULT(0),

        CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_CrtUser DEFAULT(''),
        CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_CrtDate DEFAULT('3000-01-01'),
        UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_UpdUser DEFAULT(''),
        UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_UpdDate DEFAULT('3000-01-01'),
        VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_VodUser DEFAULT(''),
        VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_TarifPolicy_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_TarifPolicy PRIMARY KEY CLUSTERED (TarifPolicyId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_TarifPolicy_Status' AND object_id = OBJECT_ID('BILRG_TarifPolicy'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_TarifPolicy_Status
    ON BILRG_TarifPolicy (PolicyStatus, CrtDate);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_TarifPolicy_PolicyNo' AND object_id = OBJECT_ID('BILRG_TarifPolicy'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_TarifPolicy_PolicyNo
    ON BILRG_TarifPolicy (PolicyNo);
END
GO
