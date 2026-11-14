CREATE TABLE BILRG_AptLoketServicePoint (
    LoketKey VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptLoketServicePoint_LoketKey DEFAULT(''),
    ServicePointId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_AptLoketServicePoint_ServicePointId DEFAULT(''),
    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptLoketServicePoint_CrtUser DEFAULT('SYSTEM'),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_AptLoketServicePoint_CrtDate DEFAULT(GETDATE()),
    CONSTRAINT PK_BILRG_AptLoketServicePoint PRIMARY KEY CLUSTERED (LoketKey, ServicePointId)
);
GO
CREATE INDEX IX_BILRG_AptLoketServicePoint_ServicePointId ON BILRG_AptLoketServicePoint(ServicePointId);
GO
