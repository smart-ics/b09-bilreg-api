-- Example seed only. Do not apply in production without Ops approval.
-- Maps former AdmissionQueueApi:Workstations and display devices.json entries.

-- INSERT INTO BILRG_AdmWorkstation
--     (WorkstationKey, DisplayName, LocationName, LoketKey, IsActive, Notes, RowVersion, CrtUser, CrtDate, UpdUser, UpdDate)
-- VALUES
--     ('PCJUDE', 'PC Jude', 'Lobby', 'LOKET-A', 1, 'Migrated from appsettings', 1, 'seed', GETDATE(), 'seed', GETDATE());

-- INSERT INTO BILRG_AdmQueueDisplay
--     (DisplayId, DisplayName, LocationName, IsActive, AudioEnabled, PollIntervalMs, LayoutKey, Notes, RowVersion, CrtUser, CrtDate, UpdUser, UpdDate)
-- VALUES
--     ('lobby-poli-1', 'Display Lobby Poli 1', 'Lobby', 1, 1, 15000, 'default', 'Migrated from devices.json', 1, 'seed', GETDATE(), 'seed', GETDATE());

-- INSERT INTO BILRG_AdmDisplayLoket (DisplayId, LoketKey, SortOrder)
-- VALUES
--     ('lobby-poli-1', 'L1', 0),
--     ('lobby-poli-1', 'L2', 1);
