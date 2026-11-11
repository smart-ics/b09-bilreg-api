-- Official seed script for Apotek Service Points (Jaminan: BPJS & Umum) and default Loket APT-RCV-01 mapping.
-- Idempotent.

SET NOCOUNT ON;

-- 1. Service Points Jalur Jaminan
IF NOT EXISTS (SELECT 1 FROM BILRG_AptServicePoint WHERE ServicePointId = 'SP-FAR-BPJS')
BEGIN
    INSERT INTO BILRG_AptServicePoint (
        ServicePointId, DepoFarmasiId, DisplayName, QueuePrefix, Active,
        CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
    )
    VALUES (
        'SP-FAR-BPJS', 'DEPO-FAR-01', 'Antrean Resep BPJS', 'A', 1,
        'SYSTEM', GETDATE(), '', '3000-01-01', '', '3000-01-01'
    );
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_AptServicePoint WHERE ServicePointId = 'SP-FAR-UMUM')
BEGIN
    INSERT INTO BILRG_AptServicePoint (
        ServicePointId, DepoFarmasiId, DisplayName, QueuePrefix, Active,
        CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
    )
    VALUES (
        'SP-FAR-UMUM', 'DEPO-FAR-01', 'Antrean Obat Umum', 'B', 1,
        'SYSTEM', GETDATE(), '', '3000-01-01', '', '3000-01-01'
    );
END;

-- 2. Default Loket Penerimaan APT-RCV-01
IF NOT EXISTS (SELECT 1 FROM BILRG_AptLoketCurrentCall WHERE LoketKey = 'APT-RCV-01')
BEGIN
    INSERT INTO BILRG_AptLoketCurrentCall (
        LoketKey, LoketType, DisplayName, DepoFarmasiId, AntrianId, NoUrut,
        ClaimState, IsActive, AnnouncementVersion, CalledAt, ServiceStartedAt, ReleasedAt,
        CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
    )
    VALUES (
        'APT-RCV-01', 1, 'Loket Penerimaan 1', 'DEPO-FAR-01', '', 0,
        0, 0, 0, '3000-01-01', '3000-01-01', '3000-01-01',
        'SYSTEM', GETDATE(), '', '3000-01-01', '', '3000-01-01'
    );
END;

-- 3. Pemetaan Awal Loket APT-RCV-01 ke Service Points (BPJS & UMUM)
IF NOT EXISTS (SELECT 1 FROM BILRG_AptLoketServicePoint WHERE LoketKey = 'APT-RCV-01' AND ServicePointId = 'SP-FAR-BPJS')
BEGIN
    INSERT INTO BILRG_AptLoketServicePoint (LoketKey, ServicePointId, CrtUser, CrtDate)
    VALUES ('APT-RCV-01', 'SP-FAR-BPJS', 'SYSTEM', GETDATE());
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_AptLoketServicePoint WHERE LoketKey = 'APT-RCV-01' AND ServicePointId = 'SP-FAR-UMUM')
BEGIN
    INSERT INTO BILRG_AptLoketServicePoint (LoketKey, ServicePointId, CrtUser, CrtDate)
    VALUES ('APT-RCV-01', 'SP-FAR-UMUM', 'SYSTEM', GETDATE());
END;
