CREATE TABLE BILRG_PreOpClearance
(
    ClearanceId       VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_ClearanceId       DEFAULT(''),
    ScheduleOpId      VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_ScheduleOpId      DEFAULT(''),
    OrderOpId         VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_OrderOpId         DEFAULT(''),
    RegId             VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_RegId             DEFAULT(''),
    NoUrut            INT          NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_NoUrut            DEFAULT(0),
    ClearanceStatus   INT          NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_ClearanceStatus   DEFAULT(0),

    -- Verifikator & Dokter PJ
    VerifikatorPpaId  VARCHAR(20)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_VerifikatorPpaId  DEFAULT(''),
    DokterPjId        VARCHAR(20)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_DokterPjId        DEFAULT(''),
    CompletedAt       DATETIME     NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_CompletedAt       DEFAULT('3000-01-01'),

    -- Emergency Override (terisi hanya jika ClearanceStatus = EMERGENCY_OVERRIDDEN)
    DokterOverrideId  VARCHAR(20)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_DokterOverrideId  DEFAULT(''),
    AlasanOverride    VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_AlasanOverride    DEFAULT(''),

    -- Not-Cleared Reason (terisi hanya jika ClearanceStatus = NOT_CLEARED)
    AlasanNotCleared  VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_AlasanNotCleared  DEFAULT(''),

    -- Audit trail standard (DATABASE.md §12)
    CrtUser           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_CrtUser           DEFAULT(''),
    CrtDate           DATETIME     NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_CrtDate           DEFAULT('3000-01-01'),
    UpdUser           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_UpdUser           DEFAULT(''),
    UpdDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_UpdDate           DEFAULT('3000-01-01'),
    VodUser           VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_VodUser           DEFAULT(''),
    VodDate           DATETIME     NOT NULL CONSTRAINT DF_BILRG_PreOpClearance_VodDate           DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_PreOpClearance PRIMARY KEY CLUSTERED (ClearanceId)
)
GO

-- Indeks untuk kueri GetLatestByScheduleOpId (OQ-002: NoUrut tertinggi per ScheduleOpId)
CREATE NONCLUSTERED INDEX IX_BILRG_PreOpClearance_ScheduleOpId_NoUrut
    ON BILRG_PreOpClearance (ScheduleOpId, NoUrut DESC)
    WHERE VodDate = '3000-01-01'
GO
