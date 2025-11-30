CREATE TABLE BILRG_Antrian
(
    AntrianId          VARCHAR(26)  NOT NULL CONSTRAINT DF_BILRG_Antrian_AntrianId DEFAULT(''),
    AntrianDate        DATETIME     NOT NULL CONSTRAINT DF_BILRG_Antrian_AntrianDate DEFAULT('3000-01-01'),
    StartTime          VARCHAR(5)   NOT NULL CONSTRAINT DF_BILRG_Antrian_StartTime DEFAULT('00:00'),
    EndTime            VARCHAR(5)   NOT NULL CONSTRAINT DF_BILRG_Antrian_EndTime DEFAULT('00:00'),
    SequenceTag        VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_Antrian_SequenceTag DEFAULT(''),
    AntrianDescription VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_Antrian_AntrianDescription DEFAULT(''),

    CONSTRAINT PK_BILRG_Antrian PRIMARY KEY CLUSTERED (AntrianId)
)
   
