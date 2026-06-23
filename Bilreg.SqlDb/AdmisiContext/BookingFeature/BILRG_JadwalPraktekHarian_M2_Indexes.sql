CREATE UNIQUE INDEX UX_BILRG_JPH_Tgl_Dokter_Jam
    ON BILRG_JadwalPraktekHarian (TglPraktek, DokterId, JamMulai)
    WHERE Status = 'ACTIVE'
    WITH (FILLFACTOR = 90);
GO

CREATE INDEX IX_BILRG_JPH_JadwalPraktekId_Tgl
    ON BILRG_JadwalPraktekHarian (JadwalPraktekId, TglPraktek)
    WITH (FILLFACTOR = 90);
GO

CREATE INDEX IX_BILRG_JPH_TglPraktek
    ON BILRG_JadwalPraktekHarian (TglPraktek, DokterId)
    WITH (FILLFACTOR = 90);
GO
