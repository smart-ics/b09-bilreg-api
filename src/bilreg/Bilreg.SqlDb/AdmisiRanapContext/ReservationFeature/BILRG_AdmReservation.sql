IF OBJECT_ID('BILRG_AdmReservation', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_AdmReservation
    (
        ReservationId     VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_ReservationId DEFAULT(''),
        ReservationStatus INT         NOT NULL CONSTRAINT DF_BILRG_AdmReservation_ReservationStatus DEFAULT(0),
        PasienId          VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_PasienId DEFAULT('-'),
        PasienName        VARCHAR(60) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_PasienName DEFAULT(''),
        TglLahir          DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmReservation_TglLahir DEFAULT('3000-01-01'),
        Gender            VARCHAR(1)  NOT NULL CONSTRAINT DF_BILRG_AdmReservation_Gender DEFAULT(''),
        OpnameRequestId   VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_OpnameRequestId DEFAULT('-'),
        PlannedDate       DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmReservation_PlannedDate DEFAULT('3000-01-01'),
        KelasId           VARCHAR(3)  NOT NULL CONSTRAINT DF_BILRG_AdmReservation_KelasId DEFAULT('-'),
        KelasName         VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_KelasName DEFAULT(''),
        BangsalId         VARCHAR(5)  NOT NULL CONSTRAINT DF_BILRG_AdmReservation_BangsalId DEFAULT('-'),
        BangsalName       VARCHAR(40) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_BangsalName DEFAULT(''),
        RealizedRegId     VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_RealizedRegId DEFAULT('-'),

        CrtUser           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_CrtUser DEFAULT(''),
        CrtDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmReservation_CrtDate DEFAULT('3000-01-01'),
        UpdUser           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_UpdUser DEFAULT(''),
        UpdDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmReservation_UpdDate DEFAULT('3000-01-01'),
        VodUser           VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_AdmReservation_VodUser DEFAULT(''),
        VodDate           DATETIME    NOT NULL CONSTRAINT DF_BILRG_AdmReservation_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_AdmReservation PRIMARY KEY CLUSTERED (ReservationId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_AdmReservation_Status_PlannedDate'
      AND object_id = OBJECT_ID('BILRG_AdmReservation'))
BEGIN
    CREATE INDEX IX_BILRG_AdmReservation_Status_PlannedDate
        ON BILRG_AdmReservation (ReservationStatus, PlannedDate)
        WITH (FILLFACTOR = 90);
END
GO
