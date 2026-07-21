-- F-03 / BR-TRK-017..018: durable recorded-order identity for append-only Tracker Events.
-- PK moves from (PasienTrackerId, EventDate) to (PasienTrackerId, NoUrut) so equal OccurredAt is allowed.
-- Orphans are profiled only; they are never deleted by this script.

-- ---------------------------------------------------------------------------
-- Profile reports (read-only). Inspect results before applying the PK change.
-- ---------------------------------------------------------------------------

-- Orphan events: rows with no matching tracker header.
SELECT
    e.PasienTrackerId,
    e.NoUrut,
    e.EventName,
    e.EventDate,
    e.ReffId
FROM BILRG_PasienTrackerEvent e
WHERE NOT EXISTS (
    SELECT 1
    FROM BILRG_PasienTracker t
    WHERE t.PasienTrackerId = e.PasienTrackerId
);

-- Duplicate recorded-order keys (must be resolved before new PK).
SELECT
    PasienTrackerId,
    NoUrut,
    COUNT(*) AS RowCount
FROM BILRG_PasienTrackerEvent
GROUP BY PasienTrackerId, NoUrut
HAVING COUNT(*) > 1;

-- Note: equal EventDate within one tracker cannot exist under the old PK (PasienTrackerId, EventDate).
GO

-- ---------------------------------------------------------------------------
-- Renumber NoUrut when duplicates exist (deterministic: EventDate, then old NoUrut).
-- Only runs when duplicate (PasienTrackerId, NoUrut) rows are present.
-- ---------------------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM BILRG_PasienTrackerEvent
    GROUP BY PasienTrackerId, NoUrut
    HAVING COUNT(*) > 1
)
BEGIN
    ;WITH Ordered AS (
        SELECT
            PasienTrackerId,
            NoUrut,
            EventDate,
            ROW_NUMBER() OVER (
                PARTITION BY PasienTrackerId
                ORDER BY EventDate, NoUrut
            ) AS NewNoUrut
        FROM BILRG_PasienTrackerEvent
    )
    UPDATE e
    SET NoUrut = o.NewNoUrut
    FROM BILRG_PasienTrackerEvent e
    INNER JOIN Ordered o
        ON o.PasienTrackerId = e.PasienTrackerId
       AND o.EventDate = e.EventDate
    WHERE e.NoUrut <> o.NewNoUrut;
END
GO

-- ---------------------------------------------------------------------------
-- Replace primary key when still on (PasienTrackerId, EventDate).
-- ---------------------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'PK_BILRG_PasienTrackerEvent'
      AND parent_object_id = OBJECT_ID('BILRG_PasienTrackerEvent')
)
AND EXISTS (
    SELECT 1
    FROM sys.index_columns ic
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
    INNER JOIN sys.key_constraints kc
        ON kc.parent_object_id = ic.object_id
       AND kc.unique_index_id = ic.index_id
    WHERE kc.name = 'PK_BILRG_PasienTrackerEvent'
      AND c.name = 'EventDate'
)
BEGIN
    ALTER TABLE BILRG_PasienTrackerEvent
        DROP CONSTRAINT PK_BILRG_PasienTrackerEvent;

    ALTER TABLE BILRG_PasienTrackerEvent
        ADD CONSTRAINT PK_BILRG_PasienTrackerEvent
        PRIMARY KEY CLUSTERED (PasienTrackerId, NoUrut);
END
GO
