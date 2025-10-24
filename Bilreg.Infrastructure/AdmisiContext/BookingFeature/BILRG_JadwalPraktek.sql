CREATE TABLE BILRG_JadwalPraktek (
    JadwalPraktekId VARCHAR(7) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_JadwalPraktekId DEFAULT (''),
    DokterId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_DokterId DEFAULT (''),
    LayananId VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_SmfId DEFAULT (''),
    Hari INT NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_Hari DEFAULT (0),
    JamMulai VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_JamMulai DEFAULT ('00:00'),
    JamSelesai VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_JamSelesai DEFAULT ('00:00'),
 
    CONSTRAINT PK_BILRG_JadwalPraktek PRIMARY KEY CLUSTERED (JadwalPraktekId)   
)
