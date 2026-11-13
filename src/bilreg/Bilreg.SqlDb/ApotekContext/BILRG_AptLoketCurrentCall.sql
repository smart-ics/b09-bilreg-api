CREATE TABLE BILRG_AptLoketCurrentCall (
    LoketKey VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_LoketKey DEFAULT(''),
    LoketType INT NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_LoketType DEFAULT(1),
    DisplayName VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_DisplayName DEFAULT(''),
    DepoFarmasiId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_DepoFarmasiId DEFAULT(''),
    AntrianId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_AntrianId DEFAULT(''),
    NoUrut INT NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_NoUrut DEFAULT(0),
    ClaimState INT NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_ClaimState DEFAULT(0),
    IsActive BIT NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_IsActive DEFAULT(0),
    AnnouncementVersion BIGINT NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_AnnouncementVersion DEFAULT(0),
    CalledAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_CalledAt DEFAULT('3000-01-01'),
    ServiceStartedAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_ServiceStartedAt DEFAULT('3000-01-01'),
    ReleasedAt DATETIME NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_ReleasedAt DEFAULT('3000-01-01'),
    RowVersion ROWVERSION NOT NULL,
    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_CrtUser DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_CrtDate DEFAULT('3000-01-01'),
    UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_UpdUser DEFAULT(''),
    UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_UpdDate DEFAULT('3000-01-01'),
    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_VodUser DEFAULT(''),
    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptLoketCurrentCall_VodDate DEFAULT('3000-01-01'),
    CONSTRAINT PK_BILRG_AptLoketCurrentCall PRIMARY KEY CLUSTERED (LoketKey)
);
GO
CREATE INDEX IX_BILRG_AptLoketCurrentCall_Depo_Type ON BILRG_AptLoketCurrentCall(DepoFarmasiId, LoketType);
GO
CREATE INDEX IX_BILRG_AptLoketCurrentCall_Queue ON BILRG_AptLoketCurrentCall(AntrianId, NoUrut);
GO
