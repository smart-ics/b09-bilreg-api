-- Seed_BILRG_RadEquipment.sql
-- Idempotent seed data for master physical radiology equipment (CT, MRI, USG, Fluoroscopy)

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM BILRG_RadEquipment WHERE EquipmentId = 'EQ-CT01')
BEGIN
    INSERT INTO BILRG_RadEquipment (EquipmentId, EquipmentName, Modality, RoomId, RoomName, IsActive)
    VALUES ('EQ-CT01', 'CT-Scan 128 Slice Siemens', 'CT', 'RAD-CT01', 'Ruang CT-Scan 1', 1);
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_RadEquipment WHERE EquipmentId = 'EQ-CT02')
BEGIN
    INSERT INTO BILRG_RadEquipment (EquipmentId, EquipmentName, Modality, RoomId, RoomName, IsActive)
    VALUES ('EQ-CT02', 'CT-Scan 64 Slice GE Optima', 'CT', 'RAD-CT02', 'Ruang CT-Scan 2', 1);
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_RadEquipment WHERE EquipmentId = 'EQ-MRI01')
BEGIN
    INSERT INTO BILRG_RadEquipment (EquipmentId, EquipmentName, Modality, RoomId, RoomName, IsActive)
    VALUES ('EQ-MRI01', 'MRI 1.5 Tesla Philips Ingenia', 'MRI', 'RAD-MRI01', 'Ruang MRI 1', 1);
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_RadEquipment WHERE EquipmentId = 'EQ-USG01')
BEGIN
    INSERT INTO BILRG_RadEquipment (EquipmentId, EquipmentName, Modality, RoomId, RoomName, IsActive)
    VALUES ('EQ-USG01', 'USG Doppler 4D Mindray', 'USG', 'RAD-USG01', 'Ruang USG 1', 1);
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_RadEquipment WHERE EquipmentId = 'EQ-USG02')
BEGIN
    INSERT INTO BILRG_RadEquipment (EquipmentId, EquipmentName, Modality, RoomId, RoomName, IsActive)
    VALUES ('EQ-USG02', 'USG Abdomen GE Voluson', 'USG', 'RAD-USG02', 'Ruang USG 2', 1);
END;

IF NOT EXISTS (SELECT 1 FROM BILRG_RadEquipment WHERE EquipmentId = 'EQ-FLR01')
BEGIN
    INSERT INTO BILRG_RadEquipment (EquipmentId, EquipmentName, Modality, RoomId, RoomName, IsActive)
    VALUES ('EQ-FLR01', 'Fluoroskopi Digital C-Arm Siemens', 'Fluoroscopy', 'RAD-FLR01', 'Ruang Fluoroskopi 1', 1);
END;
GO
