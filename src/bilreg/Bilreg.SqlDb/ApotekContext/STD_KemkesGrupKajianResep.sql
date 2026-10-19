IF OBJECT_ID('STD_KemkesGrupKajianResep') IS NULL
CREATE TABLE STD_KemkesGrupKajianResep (
    GrupKajianResepId   VARCHAR(1)  NOT NULL CONSTRAINT DF_STD_KemkesGrupKajianResep_GrupKajianResepId   DEFAULT(''),
    GrupKajianResepName VARCHAR(30) NOT NULL CONSTRAINT DF_STD_KemkesGrupKajianResep_GrupKajianResepName DEFAULT(''),
    CONSTRAINT PK_STD_KemkesGrupKajianResep PRIMARY KEY CLUSTERED (GrupKajianResepId)
);
GO
