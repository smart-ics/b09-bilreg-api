SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

CREATE TABLE BILRG_AdmDisplayLoket
(
    DisplayId VARCHAR(50) NOT NULL,
    LoketKey  VARCHAR(50) NOT NULL,
    SortOrder INT         NOT NULL CONSTRAINT DF_BILRG_AdmDisplayLoket_SortOrder DEFAULT(0),
    CONSTRAINT PK_BILRG_AdmDisplayLoket PRIMARY KEY CLUSTERED (DisplayId, LoketKey)
);

CREATE INDEX IX_BILRG_AdmDisplayLoket_LoketKey
    ON BILRG_AdmDisplayLoket(LoketKey);
