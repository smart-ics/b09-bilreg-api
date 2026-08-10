-- BILRG_StokLegacyScope.sql
-- Stock Ledger v2 — coexistence alignment / watermark per (BrgId, BrgMasukReffId).
-- Drop at cutover. ADR-STL-002: omit VodUser/VodDate.

IF OBJECT_ID('BILRG_StokLegacyScope', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_StokLegacyScope
    (
        BrgId                VARCHAR(13)  NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_BrgId DEFAULT(''),
        BrgMasukReffId       VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_BrgMasukReffId DEFAULT(''),
        AlignmentStatus      INT          NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_AlignmentStatus DEFAULT(0),
        TglMutasiLast        DATETIME     NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_TglMutasiLast DEFAULT('3000-01-01'),
        LastLegacyBukuId     VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_LastLegacyBukuId DEFAULT(''),
        LastSyncedAt         DATETIME     NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_LastSyncedAt DEFAULT('3000-01-01'),
        InconsistencyReason  VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_InconsistencyReason DEFAULT(''),

        CrtUser              VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_CrtUser DEFAULT(''),
        CrtDate              DATETIME     NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_CrtDate DEFAULT('3000-01-01'),
        UpdUser              VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_UpdUser DEFAULT(''),
        UpdDate              DATETIME     NOT NULL CONSTRAINT DF_BILRG_StokLegacyScope_UpdDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_StokLegacyScope PRIMARY KEY CLUSTERED (BrgId, BrgMasukReffId)
    );
END
GO
