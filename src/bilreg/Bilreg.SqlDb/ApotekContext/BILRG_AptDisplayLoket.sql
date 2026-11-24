CREATE TABLE BILRG_AptDisplayLoket (
    DisplayId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptDisplayLoket_DisplayId DEFAULT(''),
    LoketKey VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AptDisplayLoket_LoketKey DEFAULT(''),
    SortOrder INT NOT NULL CONSTRAINT DF_BILRG_AptDisplayLoket_SortOrder DEFAULT(0),
    CONSTRAINT PK_BILRG_AptDisplayLoket PRIMARY KEY CLUSTERED (DisplayId, LoketKey)
);
GO
CREATE INDEX IX_BILRG_AptDisplayLoket_LoketKey ON BILRG_AptDisplayLoket(LoketKey);
GO
