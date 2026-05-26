IF OBJECT_ID('BILRG_TarifVariant', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_TarifVariant
    (
        TarifPolicyId         VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_TarifVariant_TarifPolicyId DEFAULT(''),
        ItemNo                INT NOT NULL CONSTRAINT DF_BILRG_TarifVariant_ItemNo DEFAULT(0),
        TarifId               VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TarifVariant_TarifId DEFAULT(''),
        KelasId               VARCHAR(3) NOT NULL CONSTRAINT DF_BILRG_TarifVariant_KelasId DEFAULT(''),
        TipeTarifId           VARCHAR(2) NOT NULL CONSTRAINT DF_BILRG_TarifVariant_TipeTarifId DEFAULT(''),
        Nilai                 DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TarifVariant_Nilai DEFAULT(0),
        PublishedNilaiTarifId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_TarifVariant_PublishedNilaiTarifId DEFAULT(''),

        CONSTRAINT PK_BILRG_TarifVariant PRIMARY KEY CLUSTERED (TarifPolicyId, ItemNo)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_TarifVariant_VariantKey' AND object_id = OBJECT_ID('BILRG_TarifVariant'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_TarifVariant_VariantKey
    ON BILRG_TarifVariant (TarifPolicyId, TarifId, KelasId, TipeTarifId);
END
GO
