IF COL_LENGTH('BILRG_LabResultItem', 'ComponentId') IS NULL
BEGIN
    ALTER TABLE BILRG_LabResultItem
        ADD ComponentId VARCHAR(7) NOT NULL
            CONSTRAINT DF_BILRG_LabResultItem_ComponentId DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_LabResultItem', 'SequenceNo') IS NULL
BEGIN
    ALTER TABLE BILRG_LabResultItem
        ADD SequenceNo INT NOT NULL
            CONSTRAINT DF_BILRG_LabResultItem_SequenceNo DEFAULT(0);
END
GO

IF COL_LENGTH('BILRG_LabResultItem', 'IsMandatory') IS NULL
BEGIN
    ALTER TABLE BILRG_LabResultItem
        ADD IsMandatory BIT NOT NULL
            CONSTRAINT DF_BILRG_LabResultItem_IsMandatory DEFAULT(0);
END
GO
