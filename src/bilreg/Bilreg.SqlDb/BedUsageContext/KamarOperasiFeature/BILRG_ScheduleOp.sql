IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('BILRG_ScheduleOp'))
BEGIN
    CREATE TABLE BILRG_ScheduleOp
    (
        ScheduleOpId    VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_ScheduleOpId DEFAULT(''),
        ScheduleOpDate  DATETIME NOT NULL CONSTRAINT DF_BILRG_SCheduleOp_ScheduleOpDate DEFAULT('3000-01-01'), 
        OrderOpId       VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_OrderOpId DEFAULT(''),
        PasienId        VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_PasienId DEFAULT(''),
        UrgencyLevel    INT NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_UrgencyLevel DEFAULT(0),
        Durasi          INT NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_Durasi DEFAULT(0),
        TglOp           DATETIME NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_TglOp DEFAULT('3000-01-01'),
        KamarId         VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_KamarId DEFAULT(''),
        RegId           VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_RegId DEFAULT(''),
        PpaId           VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_PpaId DEFAULT(''),

        AlasanBatal     VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_AlasanBatal DEFAULT(''),
        TurnoverBuffer  INT NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_TurnoverBuffer DEFAULT(30),

        CrtUser         VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_CrtUser DEFAULT(''),
        CrtDate         DATETIME NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_CrtDate DEFAULT('3000-01-01'),
        UpdUser         VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_UpdUser DEFAULT(''),
        UpdDate         DATETIME NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_UpdDate DEFAULT('3000-01-01'),
        VodUser         VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_VodUser DEFAULT(''),
        VodDate         DATETIME NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_ScheduleOp PRIMARY KEY CLUSTERED(ScheduleOpId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('BILRG_ScheduleOp') AND name = 'IX_BILRG_ScheduleOp_OrderOpId')
BEGIN
    CREATE INDEX IX_BILRG_ScheduleOp_OrderOpId
        ON BILRG_ScheduleOp (PasienId, OrderOpId, ScheduleOpId)
        WITH(FILLFACTOR=98);
END
GO

-- Penambahan Kolom Alasan Pembatalan Resmi
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_ScheduleOp') AND name = 'AlasanBatal')
    ALTER TABLE BILRG_ScheduleOp ADD AlasanBatal VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_AlasanBatal DEFAULT('');

-- Penambahan Kolom Durasi Turnover Buffer
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_ScheduleOp') AND name = 'TurnoverBuffer')
    ALTER TABLE BILRG_ScheduleOp ADD TurnoverBuffer INT NOT NULL CONSTRAINT DF_BILRG_ScheduleOp_TurnoverBuffer DEFAULT(30);
GO

-- Index Penunjang Pengecekan Overlap Jadwal Kamar & Dokter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('BILRG_ScheduleOp') AND name = 'IX_BILRG_ScheduleOp_CollisionCheck')
    CREATE INDEX IX_BILRG_ScheduleOp_CollisionCheck
        ON BILRG_ScheduleOp (KamarId, TglOp, VodDate)
        INCLUDE (Durasi, PpaId, OrderOpId)
        WITH (FILLFACTOR = 95);
GO
