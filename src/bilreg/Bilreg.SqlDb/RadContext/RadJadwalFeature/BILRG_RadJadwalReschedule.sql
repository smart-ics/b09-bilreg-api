SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BILRG_RadJadwalReschedule')
BEGIN
    CREATE TABLE BILRG_RadJadwalReschedule (
        RescheduleId VARCHAR(16) NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_RescheduleId DEFAULT(''),
        JadwalRadId VARCHAR(16) NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_JadwalRadId DEFAULT(''),
        RescheduleIndex INT NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_RescheduleIndex DEFAULT(0),

        OldScheduleDate DATE NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_OldScheduleDate DEFAULT('3000-01-01'),
        OldStartTime TIME(0) NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_OldStartTime DEFAULT('00:00:00'),
        OldEndTime TIME(0) NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_OldEndTime DEFAULT('00:00:00'),
        OldEquipmentId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_OldEquipmentId DEFAULT(''),
        OldEquipmentName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_OldEquipmentName DEFAULT(''),

        Reason VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_Reason DEFAULT(''),
        RescheduledBy VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_RescheduledBy DEFAULT(''),
        RescheduledAt DATETIME NOT NULL CONSTRAINT DF_BILRG_RadJadwalReschedule_RescheduledAt DEFAULT('3000-01-01'),

        CONSTRAINT PK_BILRG_RadJadwalReschedule PRIMARY KEY CLUSTERED (RescheduleId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_RadJadwalReschedule_JadwalRadId' AND object_id = OBJECT_ID('BILRG_RadJadwalReschedule')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_RadJadwalReschedule_JadwalRadId
        ON BILRG_RadJadwalReschedule(JadwalRadId, RescheduleIndex);
END
GO
