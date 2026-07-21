-- Additive Service Point identity for Queue Session (F-06 / BR-TRK-026).
-- SequenceTag remains the operational lookup key; ServicePointCode is explicit session ownership.

IF COL_LENGTH('BILRG_Antrian', 'ServicePointCode') IS NULL
BEGIN
    ALTER TABLE BILRG_Antrian
        ADD ServicePointCode VARCHAR(50) NOT NULL
            CONSTRAINT DF_BILRG_Antrian_ServicePointCode DEFAULT('');
END
GO

-- Backfill from SequenceTag suffix after '_'; do not rewrite SequenceTag.
UPDATE BILRG_Antrian
SET ServicePointCode =
    CASE
        WHEN CHARINDEX('_', SequenceTag) > 0
             AND CHARINDEX('_', SequenceTag) < LEN(SequenceTag)
            THEN SUBSTRING(SequenceTag, CHARINDEX('_', SequenceTag) + 1, LEN(SequenceTag))
        ELSE ''
    END
WHERE ServicePointCode = '';
GO

-- Unique SequenceTag protects concurrent session creation used by Booking/Reg/Anonymous lookup.
IF EXISTS (
    SELECT 1
    FROM BILRG_Antrian
    GROUP BY SequenceTag
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 50001,
        'Cannot create UX_BILRG_Antrian_SequenceTag: duplicate SequenceTag values exist. Resolve duplicates before applying uniqueness.',
        1;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_BILRG_Antrian_SequenceTag'
      AND object_id = OBJECT_ID('BILRG_Antrian')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_BILRG_Antrian_SequenceTag
        ON BILRG_Antrian(SequenceTag);
END
GO
