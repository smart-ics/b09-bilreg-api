CREATE TABLE FARIN_AntrianEntry
(
	AntrianId       VARCHAR(26) NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_AntrianId DEFAULT(''),
    NoAntrian       INT         NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_NoAntrian DEFAULT(0),
    AntrianStatus   INT         NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_AntrianStatus DEFAULT(0),
    
    TakenAt         DATETIME    NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_TakenAt DEFAULT('3000-01-01'),
    AssignedAt      DATETIME    NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_AssignedAt DEFAULT('3000-01-01'),
    PreparedAt      DATETIME    NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_PreparedAt DEFAULT('3000-01-01'),
    DeliveredAt     DATETIME    NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_DeliveredAt DEFAULT('3000-01-01'),
    CancelAt        DATETIME    NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_CancelAt DEFAULT('3000-01-01'),
    
    RegId           VARCHAR(10) NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_RegId DEFAULT(''),
    PasienId        VARCHAR(15) NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_PasienId DEFAULT(''),
    PasienName      VARCHAR(60) NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_PasienName DEFAULT(''),    
	ReffId          VARCHAR(10) NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_ReffId DEFAULT(''),
    ReffDesc        VARCHAR(30) NOT NULL CONSTRAINT DF_FARIN_AntrianEntry_ReffDesc DEFAULT(''),

    CONSTRAINT PK_FARIN_AntrianEntry PRIMARY KEY CLUSTERED (AntrianId, NoAntrian)    
)
GO