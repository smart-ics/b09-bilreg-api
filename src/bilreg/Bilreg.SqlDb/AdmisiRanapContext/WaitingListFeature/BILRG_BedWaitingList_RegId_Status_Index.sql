IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_BedWaitingList_RegId_Status'
      AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
BEGIN
    CREATE INDEX IX_BILRG_BedWaitingList_RegId_Status
        ON BILRG_BedWaitingList (RegId, WaitingListStatus)
        WITH (FILLFACTOR = 90);
END
GO
