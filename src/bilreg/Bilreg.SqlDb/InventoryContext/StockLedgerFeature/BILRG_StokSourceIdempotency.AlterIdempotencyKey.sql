SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Stock Ledger Phase 3 / P3-S4 / R-002
-- Widen IdempotencyKey so P3-S1 discovery identity keys (and synthetic VOID/OMISSION forms)
-- can persist at schema-max legacy field widths without truncation.
-- Additive / backward-compatible: existing rows remain valid; unique index is recreated.
IF OBJECT_ID('dbo.BILRG_StokSourceIdempotency', 'U') IS NOT NULL
BEGIN
    DECLARE @maxLen INT =
    (
        SELECT CHARACTER_MAXIMUM_LENGTH
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'BILRG_StokSourceIdempotency'
          AND COLUMN_NAME = 'IdempotencyKey'
    );

    IF @maxLen IS NOT NULL AND @maxLen < 400
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = 'UX_BILRG_StokSourceIdempotency_Kind_Key'
              AND object_id = OBJECT_ID('dbo.BILRG_StokSourceIdempotency'))
        BEGIN
            DROP INDEX UX_BILRG_StokSourceIdempotency_Kind_Key
                ON dbo.BILRG_StokSourceIdempotency;
        END

        ALTER TABLE dbo.BILRG_StokSourceIdempotency
            ALTER COLUMN IdempotencyKey VARCHAR(400) NOT NULL;

        CREATE UNIQUE INDEX UX_BILRG_StokSourceIdempotency_Kind_Key
            ON dbo.BILRG_StokSourceIdempotency (IdempotencyKind, IdempotencyKey)
            WHERE IdempotencyKey <> '';
    END
END
GO
