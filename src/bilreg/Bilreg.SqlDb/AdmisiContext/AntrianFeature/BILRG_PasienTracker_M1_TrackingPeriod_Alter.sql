-- Additive Tracking Period columns for Patient Tracker (F-01 / BR-TRK-005..008).
-- VisitDate is retained as a legacy/compatibility field.

IF COL_LENGTH('BILRG_PasienTracker', 'StartPeriod') IS NULL
BEGIN
    ALTER TABLE BILRG_PasienTracker
        ADD StartPeriod DATETIME NOT NULL
            CONSTRAINT DF_BILRG_PasienTracker_StartPeriod DEFAULT('3000-01-01'),
            LastPeriod DATETIME NOT NULL
            CONSTRAINT DF_BILRG_PasienTracker_LastPeriod DEFAULT('3000-01-01');
END
GO

-- Backfill from verified event evidence; VisitDate is never rewritten.
UPDATE t
SET
    StartPeriod = CAST(COALESCE(e.FirstEventDate, t.VisitDate) AS DATE),
    LastPeriod = CAST(
        CASE
            WHEN e.LastEventDate IS NULL THEN t.VisitDate
            WHEN e.LastEventDate >= t.VisitDate THEN e.LastEventDate
            ELSE t.VisitDate
        END AS DATE)
FROM BILRG_PasienTracker t
OUTER APPLY (
    SELECT
        MIN(ev.EventDate) AS FirstEventDate,
        MAX(ev.EventDate) AS LastEventDate
    FROM BILRG_PasienTrackerEvent ev
    WHERE ev.PasienTrackerId = t.PasienTrackerId
) e
WHERE t.StartPeriod = '3000-01-01'
   OR t.LastPeriod = '3000-01-01';
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_BILRG_PasienTracker_TrackingPeriod'
      AND object_id = OBJECT_ID('BILRG_PasienTracker')
)
BEGIN
    CREATE INDEX IX_BILRG_PasienTracker_TrackingPeriod
        ON BILRG_PasienTracker(StartPeriod, LastPeriod);
END
GO
