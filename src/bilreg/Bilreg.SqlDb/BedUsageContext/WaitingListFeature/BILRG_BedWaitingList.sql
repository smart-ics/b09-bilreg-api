IF OBJECT_ID('BILRG_BedWaitingList', 'U') IS NULL
BEGIN
    CREATE TABLE BILRG_BedWaitingList
    (
        WaitingListId        VARCHAR(12)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_WaitingListId DEFAULT(''),
        WaitingListStatus    INT          NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_WaitingListStatus DEFAULT(0),
        RegId                VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_RegId DEFAULT('-'),
        PasienId             VARCHAR(15)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PasienId DEFAULT('-'),
        KelasId              VARCHAR(3)   NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_KelasId DEFAULT('-'),
        KelasName            VARCHAR(30)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_KelasName DEFAULT(''),
        BangsalId            VARCHAR(5)   NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_BangsalId DEFAULT('-'),
        BangsalName          VARCHAR(40)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_BangsalName DEFAULT(''),
        ArrivalSourceType    INT          NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ArrivalSourceType DEFAULT(0),
        ReferringUnitId      VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ReferringUnitId DEFAULT(''),
        ReferringUnitName    VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ReferringUnitName DEFAULT(''),
        ReferringDoctorId    VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ReferringDoctorId DEFAULT(''),
        ReferringDoctorName  VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ReferringDoctorName DEFAULT(''),
        ClinicalUrgency      INT          NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ClinicalUrgency DEFAULT(0),
        SpecialRequirements  VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_SpecialRequirements DEFAULT(''),
        PlannedBedId         VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PlannedBedId DEFAULT(''),
        PlannedBedName       VARCHAR(30)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PlannedBedName DEFAULT(''),
        PlacedBedId          VARCHAR(10)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PlacedBedId DEFAULT(''),
        PlacedPakaiBedId     VARCHAR(15)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PlacedPakaiBedId DEFAULT(''),
        CancellationReason   VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_CancellationReason DEFAULT(''),
        RerouteReason        VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_RerouteReason DEFAULT(''),
        PreviousWaitListId   VARCHAR(12)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PreviousWaitListId DEFAULT(''),
        Priority             INT          NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_Priority DEFAULT(0),

        CrtUser              VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_CrtUser DEFAULT(''),
        CrtDate              DATETIME     NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_CrtDate DEFAULT('3000-01-01'),
        UpdUser              VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_UpdUser DEFAULT(''),
        UpdDate              DATETIME     NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_UpdDate DEFAULT('3000-01-01'),
        VodUser              VARCHAR(50)  NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_VodUser DEFAULT(''),
        VodDate              DATETIME     NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_VodDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_BedWaitingList PRIMARY KEY CLUSTERED (WaitingListId)
    );
END
GO

-- Migration alter checks for existing tables (Idempotent column extensions)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'ArrivalSourceType')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD ArrivalSourceType INT NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ArrivalSourceType DEFAULT(0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'ReferringUnitId')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD ReferringUnitId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ReferringUnitId DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'ReferringUnitName')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD ReferringUnitName VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ReferringUnitName DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'ReferringDoctorId')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD ReferringDoctorId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ReferringDoctorId DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'ReferringDoctorName')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD ReferringDoctorName VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ReferringDoctorName DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'ClinicalUrgency')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD ClinicalUrgency INT NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_ClinicalUrgency DEFAULT(0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'SpecialRequirements')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD SpecialRequirements VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_SpecialRequirements DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'PlannedBedId')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD PlannedBedId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PlannedBedId DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'PlannedBedName')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD PlannedBedName VARCHAR(30) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PlannedBedName DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'PlacedBedId')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD PlacedBedId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PlacedBedId DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'PlacedPakaiBedId')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD PlacedPakaiBedId VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PlacedPakaiBedId DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'CancellationReason')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD CancellationReason VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_CancellationReason DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'RerouteReason')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD RerouteReason VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_RerouteReason DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('BILRG_BedWaitingList') 
      AND name = 'PreviousWaitListId')
BEGIN
    ALTER TABLE BILRG_BedWaitingList 
        ADD PreviousWaitListId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_BedWaitingList_PreviousWaitListId DEFAULT('');
END
GO

-- Drop legacy pasien snapshot columns if still present in legacy databases
IF COL_LENGTH('BILRG_BedWaitingList', 'PasienName') IS NOT NULL
BEGIN
    IF OBJECT_ID('DF_BILRG_BedWaitingList_PasienName') IS NOT NULL
        ALTER TABLE BILRG_BedWaitingList DROP CONSTRAINT DF_BILRG_BedWaitingList_PasienName;
    ALTER TABLE BILRG_BedWaitingList DROP COLUMN PasienName;
END
GO

IF COL_LENGTH('BILRG_BedWaitingList', 'TglLahir') IS NOT NULL
BEGIN
    IF OBJECT_ID('DF_BILRG_BedWaitingList_TglLahir') IS NOT NULL
        ALTER TABLE BILRG_BedWaitingList DROP CONSTRAINT DF_BILRG_BedWaitingList_TglLahir;
    ALTER TABLE BILRG_BedWaitingList DROP COLUMN TglLahir;
END
GO

IF COL_LENGTH('BILRG_BedWaitingList', 'Gender') IS NOT NULL
BEGIN
    IF OBJECT_ID('DF_BILRG_BedWaitingList_Gender') IS NOT NULL
        ALTER TABLE BILRG_BedWaitingList DROP CONSTRAINT DF_BILRG_BedWaitingList_Gender;
    ALTER TABLE BILRG_BedWaitingList DROP COLUMN Gender;
END
GO

-- Indeks performa worklist bangsal (Pengurutan deterministik kegawatan klinis)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_BedWaitingList_Bangsal_Status_Urgency' 
      AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
BEGIN
    CREATE INDEX IX_BILRG_BedWaitingList_Bangsal_Status_Urgency
        ON BILRG_BedWaitingList (BangsalId, WaitingListStatus, ClinicalUrgency DESC, CrtDate ASC)
        WITH (FILLFACTOR = 90);
END
GO

-- Indeks pencarian antrean aktif per registrasi
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_BedWaitingList_RegId_Status' 
      AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
BEGIN
    CREATE INDEX IX_BILRG_BedWaitingList_RegId_Status
        ON BILRG_BedWaitingList (RegId, WaitingListStatus)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_BedWaitingList_Status_CrtDate'
      AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
BEGIN
    CREATE INDEX IX_BILRG_BedWaitingList_Status_CrtDate
        ON BILRG_BedWaitingList (WaitingListStatus, CrtDate)
        WITH (FILLFACTOR = 90);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_BedWaitingList_BangsalId_Status'
      AND object_id = OBJECT_ID('BILRG_BedWaitingList'))
BEGIN
    CREATE INDEX IX_BILRG_BedWaitingList_BangsalId_Status
        ON BILRG_BedWaitingList (BangsalId, WaitingListStatus)
        WITH (FILLFACTOR = 90);
END
GO
