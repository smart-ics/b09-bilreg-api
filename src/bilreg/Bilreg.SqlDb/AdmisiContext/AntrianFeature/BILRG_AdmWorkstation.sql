SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

CREATE TABLE BILRG_AdmWorkstation
(
    WorkstationKey VARCHAR(50)  NOT NULL,
    DisplayName    VARCHAR(100) NOT NULL,
    LocationName   VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmWorkstation_LocationName DEFAULT(''),
    LoketKey       VARCHAR(50)  NOT NULL,
    IsActive       BIT          NOT NULL CONSTRAINT DF_BILRG_AdmWorkstation_IsActive DEFAULT(1),
    Notes          NVARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_AdmWorkstation_Notes DEFAULT(''),
    RowVersion     BIGINT       NOT NULL CONSTRAINT DF_BILRG_AdmWorkstation_RowVersion DEFAULT(1),
    CrtUser        VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmWorkstation_CrtUser DEFAULT(''),
    CrtDate        DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmWorkstation_CrtDate DEFAULT('3000-01-01'),
    UpdUser        VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmWorkstation_UpdUser DEFAULT(''),
    UpdDate        DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmWorkstation_UpdDate DEFAULT('3000-01-01'),
    CONSTRAINT PK_BILRG_AdmWorkstation PRIMARY KEY CLUSTERED (WorkstationKey)
);

CREATE UNIQUE INDEX UX_BILRG_AdmWorkstation_ActiveLoket
    ON BILRG_AdmWorkstation(LoketKey)
    WHERE IsActive = 1;
