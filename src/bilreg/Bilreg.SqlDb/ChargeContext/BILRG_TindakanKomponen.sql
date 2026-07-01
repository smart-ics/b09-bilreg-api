CREATE TABLE BILRG_TindakanKomponen
(
    TindakanId         VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_TindakanId DEFAULT(''),
    NoUrut             INT           NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_NoUrut DEFAULT(0),
    KomponenTarifId    VARCHAR(3)    NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_KomponenTarifId DEFAULT(''),
    KomponenTarifName  VARCHAR(30)   NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_KomponenTarifName DEFAULT(''),
    PpaId              VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_PpaId DEFAULT(''),
    PpaName            VARCHAR(60)   NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_PpaName DEFAULT(''),
    Nilai              DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_Nilai DEFAULT(0),
	Qty                DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_Qty DEFAULT(0),
    SubTotal           DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_TindakanKomponen_SubTotal DEFAULT(0),
	
	CONSTRAINT PK_BILRG_TindakanKomponen PRIMARY KEY CLUSTERED (TindakanId, KomponenTarifId)
)