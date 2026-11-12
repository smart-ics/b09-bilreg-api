IF OBJECT_ID('STD_KemkesKajianResep') IS NULL
CREATE TABLE STD_KemkesKajianResep (
    KajianResepId     VARCHAR(3)   NOT NULL CONSTRAINT DF_STD_KemkesKajianResep_KajianResepId     DEFAULT(''),
    KajianResepName   VARCHAR(128) NOT NULL CONSTRAINT DF_STD_KemkesKajianResep_KajianResepName   DEFAULT(''),
    GrupKajianResepId VARCHAR(1)   NOT NULL CONSTRAINT DF_STD_KemkesKajianResep_GrupKajianResepId  DEFAULT(''),
    DefaultIsYes      BIT          NOT NULL CONSTRAINT DF_STD_KemkesKajianResep_DefaultIsYes      DEFAULT(0),
    CONSTRAINT PK_STD_KemkesKajianResep PRIMARY KEY CLUSTERED (KajianResepId)
);
GO

IF COL_LENGTH('STD_KemkesKajianResep', 'DefaultIsYes') IS NULL
BEGIN
    ALTER TABLE STD_KemkesKajianResep
        ADD DefaultIsYes BIT NOT NULL CONSTRAINT DF_STD_KemkesKajianResep_DefaultIsYes DEFAULT(0);
END
GO
