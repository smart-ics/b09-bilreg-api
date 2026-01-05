CREATE TABLE BILRG_Tindakan
(
    TindakanId       VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_TindakanId DEFAULT(''),
    TindakanDate     DATETIME      NOT NULL CONSTRAINT DF_BILRG_Tindakan_TindakanDate DEFAULT('3000-01-01'),
	OrderTdkId       VARCHAR(26)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_OrderTdkId DEFAULT(''),
    
	RegId            VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_RegId DEFAULT(''),
    PasienId         VARCHAR(15)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_PasienId DEFAULT(''),
    PasienName       VARCHAR(60)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_PasienName DEFAULT(''),
    
	LayananId        VARCHAR(5)    NOT NULL CONSTRAINT DF_BILRG_Tindakan_LayananId DEFAULT(''),
    LayananName      VARCHAR(40)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_LayananName DEFAULT(''),
    
    KelasId          VARCHAR(3)    NOT NULL CONSTRAINT DF_BILEF_Tindakan_KelasId DEFAULT(''),
    KelasName        VARCHAR(30)    NOT NULL CONSTRAINT DF_BILEF_Tindakan_KelasName DEFAULT(''),
	TipeTarifId      VARCHAR(2)    NOT NULL CONSTRAINT DF_BILRG_Tindakan_TipeTarifId DEFAULT(''),
	TipeTarifName    VARCHAR(30)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_TipeTarifName DEFAULT(''),
	TarifId          VARCHAR(10)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_TarifId DEFAULT(''),
    TarifName        VARCHAR(55)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_TarifName DEFAULT(''),
    Total            DECIMAL(18,2) NOT NULL CONSTRAINT DF_BILRG_Tindakan_Total DEFAULT(0),
    
	CrtUser          VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_CrtUser DEFAULT(''),
    CrtDate          DATETIME      NOT NULL CONSTRAINT DF_BILRG_Tindakan_CrtDate DEFAULT('3000-01-01'),
    UpdUser          VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_UpdUser DEFAULT(''),
    UpdDate          DATETIME      NOT NULL CONSTRAINT DF_BILRG_Tindakan_UpdDate DEFAULT('3000-01-01'),
    VodUser          VARCHAR(50)   NOT NULL CONSTRAINT DF_BILRG_Tindakan_VodUser DEFAULT(''),
    VodDate          DATETIME      NOT NULL CONSTRAINT DF_BILRG_Tindakan_VodDate DEFAULT('3000-01-01'),
    
	CONSTRAINT PK_BILRG_Tindakan PRIMARY KEY CLUSTERED (TindakanId)
)