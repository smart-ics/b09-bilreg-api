-- Phase 3: server-resolved LabTestDefinition snapshot columns (replace EMR Test* fields).

IF COL_LENGTH('BILRG_LabOrderItem', 'TestDefinitionId') IS NULL
BEGIN
    ALTER TABLE BILRG_LabOrderItem ADD
        TestDefinitionId VARCHAR(7) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TestDefinitionId DEFAULT(''),
        LabTestCode VARCHAR(20) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_LabTestCode DEFAULT(''),
        LabTestName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_LabTestName DEFAULT('');
END
GO

IF COL_LENGTH('BILRG_LabOrderItem', 'TestId') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_LabOrderItem DROP CONSTRAINT DF_BILRG_LabOrderItem_TestId;
    ALTER TABLE BILRG_LabOrderItem DROP COLUMN TestId;
END
GO

IF COL_LENGTH('BILRG_LabOrderItem', 'TestCode') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_LabOrderItem DROP CONSTRAINT DF_BILRG_LabOrderItem_TestCode;
    ALTER TABLE BILRG_LabOrderItem DROP COLUMN TestCode;
END
GO

IF COL_LENGTH('BILRG_LabOrderItem', 'TestName') IS NOT NULL
BEGIN
    ALTER TABLE BILRG_LabOrderItem DROP CONSTRAINT DF_BILRG_LabOrderItem_TestName;
    ALTER TABLE BILRG_LabOrderItem DROP COLUMN TestName;
END
GO
