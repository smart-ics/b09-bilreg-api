SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BILRG_RadJadwal')
BEGIN
    CREATE TABLE BILRG_RadJadwal (
        JadwalRadId VARCHAR(16) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_JadwalRadId DEFAULT(''),
        ScheduleNo VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_ScheduleNo DEFAULT(''),
        OrderId VARCHAR(16) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_OrderId DEFAULT(''),
        OrderNo VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_OrderNo DEFAULT(''),
        RegId VARCHAR(16) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_RegId DEFAULT(''),

        PasienId VARCHAR(16) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_PasienId DEFAULT(''),
        PasienName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_PasienName DEFAULT(''),
        BirthDate DATETIME NOT NULL CONSTRAINT DF_BILRG_RadJadwal_BirthDate DEFAULT('3000-01-01'),
        Gender VARCHAR(1) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_Gender DEFAULT(''),
        Phone VARCHAR(25) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_Phone DEFAULT(''),

        Modality VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_Modality DEFAULT(''),
        EquipmentId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_EquipmentId DEFAULT(''),
        EquipmentName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_EquipmentName DEFAULT(''),
        RoomId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_RoomId DEFAULT(''),
        RoomName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_RoomName DEFAULT(''),
        DoctorRadiologyId VARCHAR(16) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_DoctorRadiologyId DEFAULT(''),
        DoctorRadiologyName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_DoctorRadiologyName DEFAULT(''),

        ScheduleDate DATE NOT NULL CONSTRAINT DF_BILRG_RadJadwal_ScheduleDate DEFAULT('3000-01-01'),
        StartTime TIME(0) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_StartTime DEFAULT('00:00:00'),
        EndTime TIME(0) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_EndTime DEFAULT('00:00:00'),
        DurationMinutes INT NOT NULL CONSTRAINT DF_BILRG_RadJadwal_DurationMinutes DEFAULT(0),
        TurnaroundBufferMinutes INT NOT NULL CONSTRAINT DF_BILRG_RadJadwal_TurnaroundBufferMinutes DEFAULT(0),

        PreparationNotes VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_PreparationNotes DEFAULT(''),
        LabRequirements VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_LabRequirements DEFAULT(''),
        SafetyChecklistJson VARCHAR(MAX) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_SafetyChecklistJson DEFAULT(''),

        IsArrived BIT NOT NULL CONSTRAINT DF_BILRG_RadJadwal_IsArrived DEFAULT(0),
        ArrivedAt DATETIME NULL,
        IsCleared BIT NOT NULL CONSTRAINT DF_BILRG_RadJadwal_IsCleared DEFAULT(0),
        ClearanceNotes VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_ClearanceNotes DEFAULT(''),
        ClearedUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_ClearedUserId DEFAULT(''),
        ClearedAt DATETIME NULL,

        Status INT NOT NULL CONSTRAINT DF_BILRG_RadJadwal_Status DEFAULT(1),
        RescheduleCount INT NOT NULL CONSTRAINT DF_BILRG_RadJadwal_RescheduleCount DEFAULT(0),
        CancellationCategory VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_CancellationCategory DEFAULT(''),
        CancellationReason VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_CancellationReason DEFAULT(''),

        CreatedUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_CreatedUserId DEFAULT(''),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_RadJadwal_CreatedDate DEFAULT('3000-01-01'),
        ModifiedUserId VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_RadJadwal_ModifiedUserId DEFAULT(''),
        ModifiedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_RadJadwal_ModifiedDate DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_RadJadwal PRIMARY KEY CLUSTERED (JadwalRadId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BILRG_RadJadwal_ScheduleNo' AND object_id = OBJECT_ID('BILRG_RadJadwal')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_BILRG_RadJadwal_ScheduleNo
        ON BILRG_RadJadwal(ScheduleNo);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_RadJadwal_OrderId' AND object_id = OBJECT_ID('BILRG_RadJadwal')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_RadJadwal_OrderId
        ON BILRG_RadJadwal(OrderId, Status);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_RadJadwal_RegId' AND object_id = OBJECT_ID('BILRG_RadJadwal')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_RadJadwal_RegId
        ON BILRG_RadJadwal(RegId) WHERE RegId <> '';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_RadJadwal_EquipmentSlot' AND object_id = OBJECT_ID('BILRG_RadJadwal')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_RadJadwal_EquipmentSlot
        ON BILRG_RadJadwal(EquipmentId, ScheduleDate, Status, StartTime, EndTime);
END
GO
