CREATE TABLE BILRG_JadwalPraktekHarian (
    JadwalPraktekHarianId VARCHAR(12) NOT NULL
        CONSTRAINT DF_BILRG_JPH_Id DEFAULT (''),
    JadwalPraktekId       VARCHAR(7)  NULL,      -- traceability; NOT a live sync link
    TglPraktek            DATE        NOT NULL,
    DokterId              VARCHAR(10) NOT NULL,
    LayananId             VARCHAR(5)  NOT NULL,
    RuangId               VARCHAR(8)  NOT NULL,
    JamMulai              VARCHAR(5)  NOT NULL,
    JamSelesai            VARCHAR(5)  NOT NULL,
    MaxPasien             INT         NOT NULL,
    AntrianPattern        VARCHAR(512) NOT NULL,
    Status                VARCHAR(20) NOT NULL,  -- ACTIVE | CANCELLED
    Source                VARCHAR(20) NOT NULL,  -- GENERATED | MANUAL
    Catatan               VARCHAR(200) NULL,     -- override reason; "Holiday" for v1 holidays

    CrtUser               VARCHAR(50) NOT NULL,
    CrtDate               DATETIME    NOT NULL,
    UpdUser               VARCHAR(50) NOT NULL,
    UpdDate               DATETIME    NOT NULL,

    CONSTRAINT PK_BILRG_JadwalPraktekHarian
        PRIMARY KEY CLUSTERED (JadwalPraktekHarianId)
);



CREATE UNIQUE INDEX UX_BILRG_JPH_Tgl_Dokter_Jam
    ON BILRG_JadwalPraktekHarian (TglPraktek, DokterId, JamMulai)
    WHERE Status = 'ACTIVE'
    WITH (FILLFACTOR = 90);

CREATE INDEX IX_BILRG_JPH_JadwalPraktekId_Tgl
    ON BILRG_JadwalPraktekHarian (JadwalPraktekId, TglPraktek)
    WITH (FILLFACTOR = 90);

CREATE INDEX IX_BILRG_JPH_TglPraktek
    ON BILRG_JadwalPraktekHarian (TglPraktek, DokterId)
    WITH (FILLFACTOR = 90);