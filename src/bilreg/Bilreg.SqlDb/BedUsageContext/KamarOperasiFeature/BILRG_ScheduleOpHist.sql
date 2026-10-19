IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('BILRG_ScheduleOpHist'))
BEGIN
    CREATE TABLE BILRG_ScheduleOpHist
    (
        ScheduleOpHistId VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_ScheduleOpHistId DEFAULT(''),
        ScheduleOpId     VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_ScheduleOpId DEFAULT(''),
        NoUrut           INT          NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_NoUrut DEFAULT(0),
        OldTglOp         DATETIME     NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_OldTglOp DEFAULT('3000-01-01'),
        NewTglOp         DATETIME     NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_NewTglOp DEFAULT('3000-01-01'),
        OldDurasi        INT          NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_OldDurasi DEFAULT(0),
        NewDurasi        INT          NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_NewDurasi DEFAULT(0),
        OldKamarId       VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_OldKamarId DEFAULT(''),
        NewKamarId       VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_NewKamarId DEFAULT(''),
        OldPpaId         VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_OldPpaId DEFAULT(''),
        NewPpaId         VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_NewPpaId DEFAULT(''),
        Alasan           VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_Alasan DEFAULT(''),
        IsOverride       BIT          NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_IsOverride DEFAULT(0),
        OverrideReason   VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_OverrideReason DEFAULT(''),
        UserId           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_UserId DEFAULT(''),
        CrtDate          DATETIME     NOT NULL CONSTRAINT DF_BILRG_ScheduleOpHist_CrtDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_ScheduleOpHist PRIMARY KEY CLUSTERED (ScheduleOpHistId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('BILRG_ScheduleOpHist') AND name = 'IX_BILRG_ScheduleOpHist_ScheduleOpId')
BEGIN
    CREATE INDEX IX_BILRG_ScheduleOpHist_ScheduleOpId
        ON BILRG_ScheduleOpHist (ScheduleOpId, NoUrut)
        WITH (FILLFACTOR = 95);
END
GO
