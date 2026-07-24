SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

CREATE TABLE BILRG_AdmQueueDisplay
(
    DisplayId      VARCHAR(50)  NOT NULL,
    DisplayName    VARCHAR(100) NOT NULL,
    LocationName   VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_LocationName DEFAULT(''),
    IsActive       BIT          NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_IsActive DEFAULT(1),
    AudioEnabled   BIT          NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_AudioEnabled DEFAULT(1),
    PollIntervalMs INT          NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_PollIntervalMs DEFAULT(15000),
    LayoutKey      VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_LayoutKey DEFAULT(''),
    Notes          NVARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_Notes DEFAULT(''),
    RowVersion     BIGINT       NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_RowVersion DEFAULT(1),
    CrtUser        VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_CrtUser DEFAULT(''),
    CrtDate        DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_CrtDate DEFAULT('3000-01-01'),
    UpdUser        VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_UpdUser DEFAULT(''),
    UpdDate        DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmQueueDisplay_UpdDate DEFAULT('3000-01-01'),
    CONSTRAINT PK_BILRG_AdmQueueDisplay PRIMARY KEY CLUSTERED (DisplayId),
    CONSTRAINT CK_BILRG_AdmQueueDisplay_PollIntervalMs CHECK (PollIntervalMs >= 1000 AND PollIntervalMs <= 120000)
);
