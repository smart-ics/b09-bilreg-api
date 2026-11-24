CREATE TABLE BILRG_AptQueueEntry (
    AntrianId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_AntrianId DEFAULT(''),
    NoUrut INT NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_NoUrut DEFAULT(0),
    ServicePointId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_ServicePointId DEFAULT(''),
    QueueLabel VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_QueueLabel DEFAULT(''),
    ApotekPhase INT NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_ApotekPhase DEFAULT(1),
    CallCountPenerimaan INT NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_CallCountPenerimaan DEFAULT(0),
    LastCalledPenerimaanAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_LastCalledPenerimaanAt DEFAULT('3000-01-01'),
    CallCountSerah INT NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_CallCountSerah DEFAULT(0),
    LastCalledSerahAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_LastCalledSerahAt DEFAULT('3000-01-01'),
    TakenAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_TakenAt DEFAULT('3000-01-01'),
    ServedAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_ServedAt DEFAULT('3000-01-01'),
    PreparedAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_PreparedAt DEFAULT('3000-01-01'),
    DoneAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_DoneAt DEFAULT('3000-01-01'),
    WithdrawnAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_WithdrawnAt DEFAULT('3000-01-01'),
    LastSkippedAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_LastSkippedAt DEFAULT('3000-01-01'),
    LastSkippedLoketType INT NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_LastSkippedLoketType DEFAULT(0),
    WithdrawalReason VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_WithdrawalReason DEFAULT(''),
    WithdrawalUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_WithdrawalUserId DEFAULT(''),
    IsPaymentOverridden BIT NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_IsPaymentOverridden DEFAULT(0),
    PaymentOverrideReason VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_PaymentOverrideReason DEFAULT(''),
    PaymentOverridePharmacistId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_PaymentOverridePharmacistId DEFAULT(''),
    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_CrtUser DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_CrtDate DEFAULT('3000-01-01'),
    UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_UpdUser DEFAULT(''),
    UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_UpdDate DEFAULT('3000-01-01'),
    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_VodUser DEFAULT(''),
    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptQueueEntry_VodDate DEFAULT('3000-01-01'),
    CONSTRAINT PK_BILRG_AptQueueEntry PRIMARY KEY CLUSTERED (AntrianId, NoUrut)
);
GO
CREATE INDEX IX_BILRG_AptQueueEntry_ServicePoint_Phase ON BILRG_AptQueueEntry(ServicePointId, ApotekPhase);
GO
CREATE INDEX IX_BILRG_AptQueueEntry_Phase ON BILRG_AptQueueEntry(ApotekPhase);
GO
