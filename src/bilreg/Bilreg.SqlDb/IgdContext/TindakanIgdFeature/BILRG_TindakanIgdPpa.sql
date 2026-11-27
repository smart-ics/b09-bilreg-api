CREATE TABLE BILRG_TindakanIgdPpa (
    TindakanIgdId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdPpa_TindakanIgdId DEFAULT(''),
    KomponenId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdPpa_KomponenId DEFAULT(''),
    PpaId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdPpa_PpaId DEFAULT(''),
    PpaName VARCHAR(45) NOT NULL CONSTRAINT DF_BILRG_TindakanIgdPpa_PpaName DEFAULT(''),

    CONSTRAINT PK_BILRG_TindakanIgdPpa PRIMARY KEY CLUSTERED (TindakanIgdId, KomponenId, PpaId)
);
GO

CREATE INDEX IX_BILRG_TindakanIgdPpa_TindakanIgdId ON BILRG_TindakanIgdPpa(TindakanIgdId);
GO
