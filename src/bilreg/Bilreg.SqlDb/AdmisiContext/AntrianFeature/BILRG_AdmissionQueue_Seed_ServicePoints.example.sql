-- BILRG_AdmissionQueue_Seed_ServicePoints.example.sql
-- EXAMPLE ONLY — replace ServicePointId / DisplayName / QueuePrefix with Ops-approved values.
-- Idempotent inserts for active Admission Service Points (ServicePointStatus = 1).
-- Active prefixes must be unique (UX_BILRG_AdmServicePoint_ActivePrefix).
-- Do not run this file unchanged against production.

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM BILRG_AdmServicePoint WHERE ServicePointId = 'ADM')
BEGIN
    INSERT BILRG_AdmServicePoint (ServicePointId, DisplayName, QueuePrefix, ServicePointStatus)
    VALUES ('ADM', 'Loket Admisi (example)', 'A', 1);
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_AdmServicePoint WHERE ServicePointId = 'ADM-B')
BEGIN
    INSERT BILRG_AdmServicePoint (ServicePointId, DisplayName, QueuePrefix, ServicePointStatus)
    VALUES ('ADM-B', 'Loket Admisi B (example)', 'B', 1);
END;

-- Optional third point — uncomment and customize after Ops approval:
-- IF NOT EXISTS (SELECT 1 FROM BILRG_AdmServicePoint WHERE ServicePointId = 'ADM-C')
-- BEGIN
--     INSERT BILRG_AdmServicePoint (ServicePointId, DisplayName, QueuePrefix, ServicePointStatus)
--     VALUES ('ADM-C', 'Loket Admisi C (example)', 'C', 1);
-- END;
