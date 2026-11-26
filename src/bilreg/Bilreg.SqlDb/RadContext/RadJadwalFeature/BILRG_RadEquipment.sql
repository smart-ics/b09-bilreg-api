SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BILRG_RadEquipment')
BEGIN
    CREATE TABLE BILRG_RadEquipment (
        EquipmentId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_RadEquipment_EquipmentId DEFAULT(''),
        EquipmentName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_RadEquipment_EquipmentName DEFAULT(''),
        Modality VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_RadEquipment_Modality DEFAULT(''),
        RoomId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_RadEquipment_RoomId DEFAULT(''),
        RoomName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_RadEquipment_RoomName DEFAULT(''),
        IsActive BIT NOT NULL CONSTRAINT DF_BILRG_RadEquipment_IsActive DEFAULT(1),

        CONSTRAINT PK_BILRG_RadEquipment PRIMARY KEY CLUSTERED (EquipmentId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BILRG_RadEquipment_Modality' AND object_id = OBJECT_ID('BILRG_RadEquipment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BILRG_RadEquipment_Modality
        ON BILRG_RadEquipment(Modality, IsActive);
END
GO
