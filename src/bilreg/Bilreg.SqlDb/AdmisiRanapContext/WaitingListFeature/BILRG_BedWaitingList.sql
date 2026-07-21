IF OBJECT_ID('BILRG_BedWaitingList', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_BedWaitingList
    (
        WaitingListId     VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_WaitingListId DEFAULT(''),
        WaitingListStatus INT         NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_WaitingListStatus DEFAULT(0),
        RegId             VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_RegId DEFAULT('-'),
        PasienId          VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PasienId DEFAULT('-'),
        KelasId           VARCHAR(3)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_KelasId DEFAULT('-'),
        KelasName         VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_KelasName DEFAULT(''),
        BangsalId         VARCHAR(5)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_BangsalId DEFAULT('-'),
        BangsalName       VARCHAR(40) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_BangsalName DEFAULT(''),
        Priority          INT         NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_Priority DEFAULT(0),

        CrtUser           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_CrtUser DEFAULT(''),
        CrtDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_CrtDate DEFAULT('3000-01-01'),
        UpdUser           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_UpdUser DEFAULT(''),
        UpdDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_UpdDate DEFAULT('3000-01-01'),
        VodUser           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_VodUser DEFAULT(''),
        VodDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_BedWaitingList PRIMARY KEY CLUSTERED (WaitingListId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_BedWaitingList_Status_CrtDate'
      AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
BEGIN
    CREATE INDEX IX_BILRG_BedWaitingList_Status_CrtDate
        ON BILRG_BedWaitingList (WaitingListStatus, CrtDate)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_BedWaitingList_BangsalId_Status'
      AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
BEGIN
    CREATE INDEX IX_BILRG_BedWaitingList_BangsalId_Status
        ON BILRG_BedWaitingList (BangsalId, WaitingListStatus)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_BedWaitingList_RegId_Status'
      AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
BEGIN
    CREATE INDEX IX_BILRG_BedWaitingList_RegId_Status
        ON BILRG_BedWaitingList (RegId, WaitingListStatus)
        WITH (FILLFACTOR = 90);
END
GO
