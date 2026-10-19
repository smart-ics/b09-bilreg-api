CREATE TABLE BILRG_RnaRoomCharge
(
    RoomChargeId            VARCHAR(30)     NOT NULL,
    PakaiBedId              VARCHAR(30)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_PakaiBedId DEFAULT(''),
    RegId                   VARCHAR(20)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_RegId DEFAULT(''),
    PasienId                VARCHAR(20)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_PasienId DEFAULT(''),
    BangsalId               VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_BangsalId DEFAULT(''),
    KamarId                 VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_KamarId DEFAULT(''),
    BedId                   VARCHAR(15)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_BedId DEFAULT(''),
    
    ChargeCycleDate         VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_ChargeCycleDate DEFAULT('3000-01-01'),
    TimeCharge              DATETIME2(0)    NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_TimeCharge DEFAULT('3000-01-01 00:00:00'),
    ChargeTriggerType       VARCHAR(25)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_ChargeTriggerType DEFAULT('DailyCutOff'),
    
    PhysicalClassId         VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_PhysicalClassId DEFAULT(''),
    PayerClassId            VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_PayerClassId DEFAULT(''),
    BillingClassId          VARCHAR(10)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_BillingClassId DEFAULT(''),
    IsTitipKelas            BIT             NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_IsTitipKelas DEFAULT(0),
    IsNaikKelas             BIT             NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_IsNaikKelas DEFAULT(0),
    DifferentialAmount      DECIMAL(18,2)   NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_DifferentialAmount DEFAULT(0),
    
    IsRoomingIn             BIT             NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_IsRoomingIn DEFAULT(0),
    MotherPakaiBedId        VARCHAR(30)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_MotherPakaiBedId DEFAULT(''),
    
    ChargedUnits            DECIMAL(5,2)    NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_ChargedUnits DEFAULT(0),
    BaseTariff              DECIMAL(18,2)   NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_BaseTariff DEFAULT(0),
    DiscountAmount          DECIMAL(18,2)   NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_DiscountAmount DEFAULT(0),
    TotalAmount             DECIMAL(18,2)   NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_TotalAmount DEFAULT(0),
    
    RoomChargeStatus        VARCHAR(15)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_RoomChargeStatus DEFAULT('Posted'),
    BillingItemRefId        VARCHAR(30)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_BillingItemRefId DEFAULT(''),
    ReversalOfRoomChargeId  VARCHAR(30)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_ReversalOfRoomChargeId DEFAULT(''),
    ReversalReason          VARCHAR(255)    NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_ReversalReason DEFAULT(''),
    
    CreatedBy               VARCHAR(20)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_CreatedBy DEFAULT(''),
    CreatedAt               DATETIME2(0)    NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_CreatedAt DEFAULT('3000-01-01 00:00:00'),
    UpdatedBy               VARCHAR(20)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_UpdatedBy DEFAULT(''),
    UpdatedAt               DATETIME2(0)    NOT NULL CONSTRAINT DF_BILRG_RnaRoomCharge_UpdatedAt DEFAULT('3000-01-01 00:00:00'),
    
    CONSTRAINT PK_BILRG_RnaRoomCharge PRIMARY KEY CLUSTERED (RoomChargeId)
);

CREATE NONCLUSTERED INDEX IX_BILRG_RnaRoomCharge_RegId ON BILRG_RnaRoomCharge (RegId, ChargeCycleDate);
CREATE NONCLUSTERED INDEX IX_BILRG_RnaRoomCharge_PakaiBedId ON BILRG_RnaRoomCharge (PakaiBedId, ChargeCycleDate);
CREATE NONCLUSTERED INDEX IX_BILRG_RnaRoomCharge_BangsalId ON BILRG_RnaRoomCharge (BangsalId, ChargeCycleDate);
