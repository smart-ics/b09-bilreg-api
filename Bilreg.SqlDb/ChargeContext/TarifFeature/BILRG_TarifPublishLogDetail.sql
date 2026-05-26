IF OBJECT_ID('BILRG_TarifPublishLogDetail', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_TarifPublishLogDetail
    (
        PublishLogId  VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_TarifPublishLogDetail_PublishLogId DEFAULT(''),
        ItemNo        INT NOT NULL CONSTRAINT DF_BILRG_TarifPublishLogDetail_ItemNo DEFAULT(0),
        TarifId       VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TarifPublishLogDetail_TarifId DEFAULT(''),
        KelasId       VARCHAR(3) NOT NULL CONSTRAINT DF_BILRG_TarifPublishLogDetail_KelasId DEFAULT(''),
        TipeTarifId   VARCHAR(2) NOT NULL CONSTRAINT DF_BILRG_TarifPublishLogDetail_TipeTarifId DEFAULT(''),
        NilaiTarifId  VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_TarifPublishLogDetail_NilaiTarifId DEFAULT(''),
        Nilai         DECIMAL(18, 2) NOT NULL CONSTRAINT DF_BILRG_TarifPublishLogDetail_Nilai DEFAULT(0),

        CONSTRAINT PK_BILRG_TarifPublishLogDetail PRIMARY KEY CLUSTERED (PublishLogId, ItemNo)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_TarifPublishLogDetail_Log' AND object_id = OBJECT_ID('BILRG_TarifPublishLogDetail'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_TarifPublishLogDetail_Log
    ON BILRG_TarifPublishLogDetail (PublishLogId);
END
GO
