-- M03-F01 P1-S02 — order-type to accountable receiving-service mapping.
-- Idempotent: safe to re-run. Ops may retarget a LayananId/Name or
-- deactivate a row without touching code.
-- OrderState lifecycle (reference only): 0=Proposed, 1=Sent, 2=Received, 3=Cancelled.

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM trs_tujuan_lanjut WHERE OrderType = 'Kontrol')
BEGIN
    INSERT INTO trs_tujuan_lanjut (OrderType, LayananId, LayananName, IsActive)
    VALUES ('Kontrol', '', '', 1);
END;

IF NOT EXISTS (SELECT 1 FROM trs_tujuan_lanjut WHERE OrderType = 'Penunjang')
BEGIN
    INSERT INTO trs_tujuan_lanjut (OrderType, LayananId, LayananName, IsActive)
    VALUES ('Penunjang', '', '', 1);
END;

IF NOT EXISTS (SELECT 1 FROM trs_tujuan_lanjut WHERE OrderType = 'Transfer')
BEGIN
    INSERT INTO trs_tujuan_lanjut (OrderType, LayananId, LayananName, IsActive)
    VALUES ('Transfer', '', '', 1);
END;

IF NOT EXISTS (SELECT 1 FROM trs_tujuan_lanjut WHERE OrderType = 'Operasi')
BEGIN
    INSERT INTO trs_tujuan_lanjut (OrderType, LayananId, LayananName, IsActive)
    VALUES ('Operasi', '', '', 1);
END;

IF NOT EXISTS (SELECT 1 FROM trs_tujuan_lanjut WHERE OrderType = 'RawatInap')
BEGIN
    INSERT INTO trs_tujuan_lanjut (OrderType, LayananId, LayananName, IsActive)
    VALUES ('RawatInap', '', '', 1);
END;
