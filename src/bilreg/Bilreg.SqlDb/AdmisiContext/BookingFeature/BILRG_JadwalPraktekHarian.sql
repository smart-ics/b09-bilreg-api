CREATE TABLE BILRG_JadwalPraktekHarian (
    JadwalPraktekHarianId VARCHAR(12) NOT NULL
        CONSTRAINT DF_BILRG_JPH_Id DEFAULT (''),
    JadwalPraktekId       VARCHAR(7)  NULL,
    TglPraktek            DATE        NOT NULL,
    DokterId              VARCHAR(10) NOT NULL,
    LayananId             VARCHAR(5)  NOT NULL,
    RuangId               VARCHAR(8)  NOT NULL,
    JamMulai              VARCHAR(5)  NOT NULL,
    JamSelesai            VARCHAR(5)  NOT NULL,
    MaxPasien             INT         NOT NULL,
    AntrianPattern        VARCHAR(512) NOT NULL,
    Status                VARCHAR(20) NOT NULL,
    Source                VARCHAR(20) NOT NULL,
    Catatan               VARCHAR(200) NULL,

    CrtUser               VARCHAR(50) NOT NULL,
    CrtDate               DATETIME    NOT NULL,
    UpdUser               VARCHAR(50) NOT NULL,
    UpdDate               DATETIME    NOT NULL,

    CONSTRAINT PK_BILRG_JadwalPraktekHarian
        PRIMARY KEY CLUSTERED (JadwalPraktekHarianId)
)
GO
