CREATE TABLE [dbo].[BILRG_DoInspeksi] (
    [DoId]              VARCHAR(10)     NOT NULL,
    [NoUrut]            INT             NOT NULL,
    [BrgId]             VARCHAR(13)     NOT NULL,
    [PoNoUrut]          INT             NOT NULL CONSTRAINT [DF_BILRG_DoInspeksi_PoNoUrut] DEFAULT (0),
    [QtyKirim]          DECIMAL(18, 2)  NOT NULL CONSTRAINT [DF_BILRG_DoInspeksi_QtyKirim] DEFAULT (0),
    [QtyDiterima]       DECIMAL(18, 2)  NOT NULL CONSTRAINT [DF_BILRG_DoInspeksi_QtyDiterima] DEFAULT (0),
    [QtyDitolak]        DECIMAL(18, 2)  NOT NULL CONSTRAINT [DF_BILRG_DoInspeksi_QtyDitolak] DEFAULT (0),
    [AlasanTolak]       VARCHAR(200)    NOT NULL CONSTRAINT [DF_BILRG_DoInspeksi_AlasanTolak] DEFAULT (''),
    [IsBonus]           BIT             NOT NULL CONSTRAINT [DF_BILRG_DoInspeksi_IsBonus] DEFAULT (0),
    [NoSuratJaminan]    VARCHAR(50)     NOT NULL CONSTRAINT [DF_BILRG_DoInspeksi_NoSuratJaminan] DEFAULT (''),
    [Status]            VARCHAR(20)     NOT NULL CONSTRAINT [DF_BILRG_DoInspeksi_Status] DEFAULT ('Draft'),
    [ConfirmedAt]       DATETIME        NULL,
    [ConfirmedBy]       VARCHAR(20)     NOT NULL CONSTRAINT [DF_BILRG_DoInspeksi_ConfirmedBy] DEFAULT (''),

    CONSTRAINT [PK_BILRG_DoInspeksi] PRIMARY KEY CLUSTERED ([DoId] ASC, [NoUrut] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_BILRG_DoInspeksi_BrgId] 
    ON [dbo].[BILRG_DoInspeksi] ([BrgId] ASC);
GO
