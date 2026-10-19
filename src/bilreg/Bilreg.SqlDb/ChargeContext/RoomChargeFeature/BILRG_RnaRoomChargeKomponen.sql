CREATE TABLE BILRG_RnaRoomChargeKomponen
(
    RoomChargeId            VARCHAR(30)     NOT NULL,
    KomponenId              VARCHAR(10)     NOT NULL,
    KomponenName            VARCHAR(100)    NOT NULL CONSTRAINT DF_BILRG_RnaRoomChargeKomponen_KomponenName DEFAULT(''),
    KomponenCategory        VARCHAR(30)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomChargeKomponen_KomponenCategory DEFAULT('Akomodasi'),
    Tarif                   DECIMAL(18,2)   NOT NULL CONSTRAINT DF_BILRG_RnaRoomChargeKomponen_Tarif DEFAULT(0),
    Diskon                  DECIMAL(18,2)   NOT NULL CONSTRAINT DF_BILRG_RnaRoomChargeKomponen_Diskon DEFAULT(0),
    Total                   DECIMAL(18,2)   NOT NULL CONSTRAINT DF_BILRG_RnaRoomChargeKomponen_Total DEFAULT(0),
    RekeningPendapatan      VARCHAR(25)     NOT NULL CONSTRAINT DF_BILRG_RnaRoomChargeKomponen_RekeningPendapatan DEFAULT(''),
    
    CONSTRAINT PK_BILRG_RnaRoomChargeKomponen PRIMARY KEY CLUSTERED (RoomChargeId, KomponenId)
);
