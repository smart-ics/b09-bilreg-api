SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

CREATE TABLE BILRG_ClinicQueueCall
(
    PoliId     VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_PoliId DEFAULT(''),
    DokterId   VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_DokterId DEFAULT(''),
    RuangId    VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_RuangId DEFAULT(''),
    AntrianId  VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_AntrianId DEFAULT(''),
    NoUrut     INT         NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_NoUrut DEFAULT(0),
    CalledAt   DATETIME    NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_CalledAt DEFAULT('3000-01-01'),
    CallCount  INT         NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_CallCount DEFAULT(1),
    CallStatus INT         NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_CallStatus DEFAULT(0), -- 0: Active, 1: Skipped, 2: Dismissed
    UserId     VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_UserId DEFAULT(''),
    CrtDate    DATETIME    NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_CrtDate DEFAULT('3000-01-01'),
    UpdDate    DATETIME    NOT NULL CONSTRAINT DF_BILRG_ClinicQueueCall_UpdDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_ClinicQueueCall PRIMARY KEY CLUSTERED (PoliId, DokterId)
);

CREATE INDEX IX_BILRG_ClinicQueueCall_Lookup
    ON BILRG_ClinicQueueCall(PoliId, CallStatus)
    INCLUDE (DokterId, RuangId, AntrianId, NoUrut, CalledAt);
