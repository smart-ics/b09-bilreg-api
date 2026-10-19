IF OBJECT_ID('BILRG_AptTelaahResepKajian') IS NULL
CREATE TABLE BILRG_AptTelaahResepKajian (
    TelaahResepId    VARCHAR(12)   NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepKajian_TelaahResepId    DEFAULT(''),
    NoUrut           INT           NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepKajian_NoUrut           DEFAULT(0),
    KajianResepId    VARCHAR(3)    NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepKajian_KajianResepId    DEFAULT(''),
    KajianResepName  VARCHAR(128)  NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepKajian_KajianResepName  DEFAULT(''),
    GrupKajianResepId   VARCHAR(1) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepKajian_GrupKajianResepId   DEFAULT(''),
    GrupKajianResepName VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepKajian_GrupKajianResepName DEFAULT(''),
    IsYes            BIT           NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepKajian_IsYes            DEFAULT(0),
    CatatanKajian    VARCHAR(512)  NOT NULL CONSTRAINT DF_BILRG_AptTelaahResepKajian_CatatanKajian    DEFAULT(''),
    CONSTRAINT PK_BILRG_AptTelaahResepKajian PRIMARY KEY CLUSTERED (TelaahResepId, NoUrut)
);
GO
