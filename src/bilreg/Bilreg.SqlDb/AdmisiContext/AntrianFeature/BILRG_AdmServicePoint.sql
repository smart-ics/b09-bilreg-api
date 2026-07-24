SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

CREATE TABLE BILRG_AdmServicePoint
(
    ServicePointId     VARCHAR(50)  NOT NULL,
    DisplayName        VARCHAR(100) NOT NULL,
    QueuePrefix        CHAR(1)      NOT NULL,
    ServicePointStatus INT          NOT NULL,
    CrtUser            VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmServicePoint_CrtUser DEFAULT(''),
    CrtDate            DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmServicePoint_CrtDate DEFAULT('3000-01-01'),
    UpdUser            VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmServicePoint_UpdUser DEFAULT(''),
    UpdDate            DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmServicePoint_UpdDate DEFAULT('3000-01-01'),
    VodUser            VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_AdmServicePoint_VodUser DEFAULT(''),
    VodDate            DATETIME     NOT NULL CONSTRAINT DF_BILRG_AdmServicePoint_VodDate DEFAULT('3000-01-01'),
    CONSTRAINT PK_BILRG_AdmServicePoint PRIMARY KEY CLUSTERED (ServicePointId),
    CONSTRAINT CK_BILRG_AdmServicePoint_Prefix CHECK (QueuePrefix LIKE '[A-Z]' COLLATE Latin1_General_100_BIN2),
    CONSTRAINT CK_BILRG_AdmServicePoint_Status CHECK (ServicePointStatus IN (0, 1))
);

CREATE UNIQUE INDEX UX_BILRG_AdmServicePoint_ActivePrefix
    ON BILRG_AdmServicePoint(QueuePrefix)
    WHERE ServicePointStatus = 1;
