CREATE TABLE BILRG_NilaiTarif
(
    NilaiTarifId   VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_NilaiTarif_NilaiTarifId DEFAULT(''),
    TarifId        VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_NilaiTarif_TarifId DEFAULT(''),
    TipeTarifId    VARCHAR(2) NOT NULL CONSTRAINT DF_BILRG_NilaiTarif_TipeTarifId DEFAULT(''),
    KelasId        VARCHAR(3) NOT NULL CONSTRAINT DF_BILRG_NilaiTarif_KelasId DEFAULT(''),
    Nilai          DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_NilaiTarif_Nilai DEFAULT(0),
    SourcePolicyId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_NilaiTarif_SourcePolicyId DEFAULT(''),
    
    CONSTRAINT PK_BILRG_NilaiTarif PRIMARY KEY CLUSTERED(NilaiTarifId)
)
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_BILRG_NilaiTarif_Variant
ON BILRG_NilaiTarif (TarifId, TipeTarifId, KelasId);
GO
