-- EXAMPLE ONLY — replace ServicePointId / DisplayName / QueuePrefix with Ops-approved values.
-- Pharmacy service point for shared Patient Tracker queue infrastructure.
-- Active prefixes must be unique (UX_BILRG_AdmServicePoint_ActivePrefix).
-- Do not run this file unchanged against production.

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM BILRG_AdmServicePoint WHERE ServicePointId = 'APT')
BEGIN
    INSERT BILRG_AdmServicePoint (ServicePointId, DisplayName, QueuePrefix, ServicePointStatus)
    VALUES ('APT', 'Apotek Rawat Jalan (example)', 'P', 1);
END;
