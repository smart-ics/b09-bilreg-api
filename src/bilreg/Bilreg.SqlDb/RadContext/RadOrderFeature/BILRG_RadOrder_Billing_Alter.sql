-- Migration: Add Billing Columns to BILRG_RadOrder
IF COL_LENGTH('BILRG_RadOrder', 'BillingTindakanId') IS NULL
BEGIN
    ALTER TABLE BILRG_RadOrder ADD
        BillingTindakanId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_RadOrder_BillingTindakanId DEFAULT(''),
        BillingLastError VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_RadOrder_BillingLastError DEFAULT('');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_BILRG_RadOrder_Billing' AND object_id = OBJECT_ID('BILRG_RadOrder')
)
BEGIN
    CREATE INDEX IX_BILRG_RadOrder_Billing ON BILRG_RadOrder(BillingTindakanId) WHERE BillingTindakanId <> '';
END
GO
