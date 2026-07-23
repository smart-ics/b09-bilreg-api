CREATE TABLE BILRG_Antrian
(
    AntrianId          VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_Antrian_AntrianId DEFAULT(''),
    AntrianDate        DATETIME     NOT NULL CONSTRAINT DF_BILRG_Antrian_AntrianDate DEFAULT('3000-01-01'),
    StartTime          VARCHAR(5)   NOT NULL CONSTRAINT DF_BILRG_Antrian_StartTime DEFAULT('00:00'),
    EndTime            VARCHAR(5)   NOT NULL CONSTRAINT DF_BILRG_Antrian_EndTime DEFAULT('00:00'),
    SequenceTag        VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_Antrian_SequenceTag DEFAULT(''),
    AntrianDescription VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_Antrian_AntrianDescription DEFAULT(''),
    ServicePointCode   VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_Antrian_ServicePointCode DEFAULT(''),
    QueuePrefixSnapshot CHAR(1)     NOT NULL CONSTRAINT DF_BILRG_Antrian_QueuePrefixSnapshot DEFAULT(''),
    CrtUser            VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_Antrian_CrtUser DEFAULT(''),
    CrtDate            DATETIME     NOT NULL CONSTRAINT DF_BILRG_Antrian_CrtDate DEFAULT('3000-01-01'),
    UpdUser            VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_Antrian_UpdUser DEFAULT(''),
    UpdDate            DATETIME     NOT NULL CONSTRAINT DF_BILRG_Antrian_UpdDate DEFAULT('3000-01-01'),
    VodUser            VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_Antrian_VodUser DEFAULT(''),
    VodDate            DATETIME     NOT NULL CONSTRAINT DF_BILRG_Antrian_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_Antrian PRIMARY KEY CLUSTERED (AntrianId)
)
   
