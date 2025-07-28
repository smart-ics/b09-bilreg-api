CREATE TABLE BILRG_JadwalPraktek (
    JadwalPraktekId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_JadwalPraktekId DEFAULT (''),
    DokterId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_DokterId DEFAULT (''),
    SmfId VARCHAR(2) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_SmfId DEFAULT (''),
    Hari INT NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_Hari DEFAULT (0),
    JamMulai VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_JamMulai DEFAULT (''),
    JamSelesai VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_JadwalPraktek_JamSelesai DEFAULT (''),
 
    CONSTRAINT PK_BILRG_JadwalPraktek PRIMARY KEY CLUSTERED (JadwalPraktekId)   
)
