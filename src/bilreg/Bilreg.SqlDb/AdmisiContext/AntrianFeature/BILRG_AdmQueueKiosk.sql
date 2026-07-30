SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

CREATE TABLE BILRG_AdmQueueKiosk
(
    StationId        VARCHAR(50)   NOT NULL,
    DisplayName      VARCHAR(100)  NOT NULL,
    LocationName     VARCHAR(100)  NOT NULL CONSTRAINT DF_BILRG_AdmQueueKiosk_LocationName DEFAULT(''),
    IsActive         BIT           NOT NULL CONSTRAINT DF_BILRG_AdmQueueKiosk_IsActive DEFAULT(1),
    PrinterProxyPort INT           NOT NULL CONSTRAINT DF_BILRG_AdmQueueKiosk_PrinterProxyPort DEFAULT(5050),
    Notes             NVARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_AdmQueueKiosk_Notes DEFAULT(''),
    RowVersion        BIGINT        NOT NULL CONSTRAINT DF_BILRG_AdmQueueKiosk_RowVersion DEFAULT(1),
    CrtUser           VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_AdmQueueKiosk_CrtUser DEFAULT(''),
    CrtDate           DATETIME      NOT NULL CONSTRAINT DF_BILRG_AdmQueueKiosk_CrtDate DEFAULT('3000-01-01'),
    UpdUser           VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_AdmQueueKiosk_UpdUser DEFAULT(''),
    UpdDate           DATETIME      NOT NULL CONSTRAINT DF_BILRG_AdmQueueKiosk_UpdDate DEFAULT('3000-01-01'),
    CONSTRAINT PK_BILRG_AdmQueueKiosk PRIMARY KEY CLUSTERED (StationId),
    CONSTRAINT CK_BILRG_AdmQueueKiosk_PrinterProxyPort CHECK (PrinterProxyPort BETWEEN 1 AND 65535)
);
